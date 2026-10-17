import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRight, MessageCircleQuestion } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { agentRecoveryApi } from "../agent-recovery.api";
import type {
  ImpactNode,
  ImpactReviewDecision,
} from "../agent-recovery.types";
import { ErrorLine, SourceBadge } from "./copilot-shared";
import { time } from "./copilot-utils";

const laneTitle: Record<ImpactNode["lane"], string> = {
  Bus: "Same bus: where it is needed next",
  Driver: "Same driver: duties after this trip",
  Connection: "Passenger connections at the destination",
  Headway: "Buses leaving close together",
};
const riskStyle = {
  High: "border-red-300 bg-red-50",
  Medium: "border-amber-300 bg-amber-50",
  Low: "border-emerald-300 bg-emerald-50/60",
} as const;

export function NetworkPanel({ workflowId }: { workflowId: string }) {
  const queryClient = useQueryClient();
  const key = ["agent-recovery", workflowId, "impact"];
  const impact = useQuery({
    queryKey: key,
    queryFn: () => agentRecoveryApi.impact(workflowId),
    retry: false,
  });
  const review = useMutation({
    mutationFn: (input: { tripId: string; decision: ImpactReviewDecision }) =>
      agentRecoveryApi.reviewImpact(workflowId, input.tripId, input.decision),
    onSuccess: (fresh) => queryClient.setQueryData(key, fresh),
  });

  if (impact.isLoading)
    return (
      <p className="text-sm text-muted-foreground">
        The network agent is following the delay through the timetable...
      </p>
    );
  if (impact.error || !impact.data) return <ErrorLine error={impact.error} />;

  const { summary, nodes, recommendation } = impact.data;
  const lanes = (["Bus", "Driver", "Connection", "Headway"] as const)
    .map((lane) => ({
      lane,
      nodes: nodes.filter((node) => node.lane === lane).sort((a, b) => a.depth - b.depth),
    }))
    .filter((group) => group.nodes.length > 0);

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-indigo-200 bg-indigo-50/60 p-4 text-sm">
        <p className="font-medium text-indigo-900">
          Recovered service runs {time(impact.data.recoveredDeparture)} →{" "}
          {time(impact.data.recoveredArrival)} (+{impact.data.delayMinutes} min)
        </p>
        <p className="mt-1 text-indigo-950">{recommendation}</p>
        <div className="mt-3 flex flex-wrap gap-2">
          <SourceBadge label={`${summary.high} high`} tone={summary.high ? "warning" : "neutral"} />
          <SourceBadge label={`${summary.medium} medium`} />
          <SourceBadge label={`${summary.low} low`} tone="good" />
          {summary.escalated > 0 && <SourceBadge label={`${summary.escalated} escalated`} tone="info" />}
          {summary.ignored > 0 && <SourceBadge label={`${summary.ignored} ignored`} />}
          <SourceBadge label={`${summary.passengersAtRisk} passengers at risk`} />
        </div>
      </div>

      {lanes.length === 0 && (
        <p className="text-sm text-muted-foreground">
          No other trip is disturbed by this recovery.
        </p>
      )}
      {lanes.map((group) => (
        <section key={group.lane}>
          <h3 className="text-sm font-semibold">{laneTitle[group.lane]}</h3>
          <div className="mt-2 flex flex-wrap items-stretch gap-2">
            {group.nodes.map((node, index) => (
              <div key={`${node.lane}-${node.tripId}`} className="flex items-center gap-2">
                {index > 0 && group.lane !== "Connection" && group.lane !== "Headway" && (
                  <ArrowRight className="size-4 text-muted-foreground" />
                )}
                <NodeCard
                  workflowId={workflowId}
                  node={node}
                  busy={review.isPending}
                  onReview={(decision) => review.mutate({ tripId: node.tripId, decision })}
                />
              </div>
            ))}
          </div>
        </section>
      ))}
      <ErrorLine error={review.error} />
    </div>
  );
}

function NodeCard({
  workflowId,
  node,
  busy,
  onReview,
}: {
  workflowId: string;
  node: ImpactNode;
  busy: boolean;
  onReview: (decision: ImpactReviewDecision) => void;
}) {
  const [question, setQuestion] = useState("");
  const [open, setOpen] = useState(false);
  const explain = useMutation({
    mutationFn: () => agentRecoveryApi.explainImpact(workflowId, node.tripId, question),
  });
  const ignored = node.review === "Ignored";
  return (
    <article
      className={cn(
        "w-64 rounded-lg border p-3 text-sm",
        riskStyle[node.risk],
        ignored && "opacity-50",
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <p className="font-semibold">
          Route {node.routeNumber} · {time(node.scheduledTime)}
        </p>
        <SourceBadge
          label={node.review ?? node.risk}
          tone={node.risk === "High" ? "warning" : node.review === "Escalated" ? "info" : "neutral"}
        />
      </div>
      <p className="mt-1 text-xs text-muted-foreground">{node.reason}</p>
      <p className="mt-1 text-xs text-muted-foreground">
        {node.expectedLateMinutes > 0 && `${node.expectedLateMinutes} min late · `}
        {node.passengers} passengers
      </p>
      <div className="mt-2 flex flex-wrap gap-1">
        <Button size="xs" variant="outline" onClick={() => setOpen((value) => !value)}>
          <MessageCircleQuestion /> Why?
        </Button>
        <Button size="xs" variant="outline" disabled={busy} onClick={() => onReview("Escalate")}>
          Escalate
        </Button>
        <Button size="xs" variant="outline" disabled={busy} onClick={() => onReview("Ignore")}>
          Ignore
        </Button>
        {node.review && (
          <Button size="xs" variant="ghost" disabled={busy} onClick={() => onReview("Reset")}>
            Reset
          </Button>
        )}
      </div>
      {open && (
        <div className="mt-2 space-y-2 border-t border-slate-200 pt-2">
          <div className="flex gap-1">
            <input
              className="h-8 min-w-0 flex-1 rounded-md border bg-background px-2 text-xs"
              placeholder="Ask the network agent (optional)"
              value={question}
              maxLength={500}
              onChange={(event) => setQuestion(event.target.value)}
            />
            <Button size="xs" disabled={explain.isPending} onClick={() => explain.mutate()}>
              Ask
            </Button>
          </div>
          {explain.data && (
            <div className="rounded-md bg-white/80 p-2 text-xs">
              <p>{explain.data.answer}</p>
              <p className="mt-1">
                <SourceBadge
                  label={explain.data.source === "ai" ? "Gemini" : "Rule-based"}
                  tone={explain.data.source === "ai" ? "good" : "neutral"}
                />
              </p>
            </div>
          )}
          <ErrorLine error={explain.error} />
        </div>
      )}
    </article>
  );
}
