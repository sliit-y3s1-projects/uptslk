import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, FlaskConical } from "lucide-react";
import { Button } from "@/components/ui/button";
import { agentRecoveryApi } from "../agent-recovery.api";
import type { RecoveryAlternative } from "../agent-recovery.types";
import { ErrorLine, ScoreBar, SourceBadge } from "./copilot-shared";
import { time } from "./copilot-utils";

export function FleetPanel({ workflowId }: { workflowId: string }) {
  const alternatives = useQuery({
    queryKey: ["agent-recovery", workflowId, "alternatives"],
    queryFn: () => agentRecoveryApi.alternatives(workflowId),
    retry: false,
  });

  if (alternatives.isLoading)
    return (
      <p className="text-sm text-muted-foreground">
        The fleet agent is simulating options...
      </p>
    );
  if (alternatives.error || !alternatives.data)
    return <ErrorLine error={alternatives.error} />;

  const { recommendation, evaluated, alternatives: options } = alternatives.data;
  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-sky-200 bg-sky-50/60 p-4 text-sm">
        <p className="flex items-center gap-2 font-medium text-sky-900">
          <FlaskConical className="size-4" /> Fleet agent simulated {evaluated}{" "}
          scenarios against the live timetable
        </p>
        <p className="mt-1 text-sky-950">{recommendation}</p>
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        {options.map((option, index) => (
          <AlternativeCard
            key={option.id}
            workflowId={workflowId}
            option={option}
            rank={index + 1}
          />
        ))}
      </div>
      {options.length === 0 && (
        <p className="text-sm text-muted-foreground">
          No other feasible option was found.
        </p>
      )}
    </div>
  );
}

function AlternativeCard({
  workflowId,
  option,
  rank,
}: {
  workflowId: string;
  option: RecoveryAlternative;
  rank: number;
}) {
  const queryClient = useQueryClient();
  const [delay, setDelay] = useState(option.delayMinutes);
  const [tweaked, setTweaked] = useState<RecoveryAlternative | null>(null);
  const shown = tweaked ?? option;
  const scenario = {
    vehicleId: option.vehicleId,
    driverId: option.driverId,
    bayId: option.bayId,
    delayMinutes: delay,
  };
  const simulate = useMutation({
    mutationFn: () => agentRecoveryApi.simulate(workflowId, scenario),
    onSuccess: setTweaked,
  });
  const apply = useMutation({
    mutationFn: () => agentRecoveryApi.applyAlternative(workflowId, scenario),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["agent-recovery"] }),
  });
  const usable = shown.feasible && !(shown.isCurrent && delay === option.delayMinutes);

  return (
    <article
      className={`rounded-lg border p-4 ${shown.isCurrent ? "border-sky-400 bg-sky-50/40" : "border-slate-300 bg-card"}`}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="flex flex-wrap items-center gap-2 font-semibold">
            #{rank} {shown.vehiclePlate}
            {shown.isCurrent && <SourceBadge label="Current proposal" tone="info" />}
          </p>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {shown.vehicleCapacity} seats · {shown.driverName ?? "no driver"} · bay{" "}
            {shown.bayCode ?? "?"}
          </p>
          <p className="text-sm text-muted-foreground">
            Departs {time(shown.scheduledTime)} (+{shown.delayMinutes} min)
          </p>
        </div>
        <div className="text-right">
          <p className="text-2xl font-semibold tabular-nums">
            {shown.feasible ? shown.score : "—"}
          </p>
          <p className="text-[11px] text-muted-foreground">score / 100</p>
        </div>
      </div>

      {shown.feasible ? (
        <div className="mt-3 grid gap-2 sm:grid-cols-2">
          {shown.factors.map((factor) => (
            <ScoreBar
              key={factor.factor}
              label={factor.factor}
              points={factor.points}
              max={factor.maxPoints}
              detail={factor.detail}
            />
          ))}
        </div>
      ) : (
        <ul className="mt-3 space-y-1 rounded-md bg-red-50 p-3 text-sm text-red-900">
          {shown.issues.map((issue) => (
            <li key={issue}>• {issue}</li>
          ))}
        </ul>
      )}
      {shown.risks.length > 0 && (
        <ul className="mt-3 space-y-1 rounded-md bg-amber-50 p-3 text-xs text-amber-900">
          {shown.risks.map((risk) => (
            <li key={risk}>⚠ {risk}</li>
          ))}
        </ul>
      )}

      <div className="mt-4 flex flex-wrap items-center gap-3 border-t border-slate-200 pt-3">
        <label className="flex flex-1 items-center gap-2 text-xs text-muted-foreground">
          Delay
          <input
            type="range"
            min={0}
            max={120}
            step={5}
            value={delay}
            onChange={(event) => setDelay(Number(event.target.value))}
            className="min-w-24 flex-1"
          />
          <span className="w-12 tabular-nums">{delay} min</span>
        </label>
        <Button
          size="sm"
          variant="outline"
          disabled={simulate.isPending}
          onClick={() => simulate.mutate()}
        >
          Re-score
        </Button>
        <Button
          size="sm"
          disabled={apply.isPending || !usable}
          onClick={() => apply.mutate()}
        >
          <Check /> Use this
        </Button>
      </div>
      <ErrorLine error={simulate.error ?? apply.error} />
    </article>
  );
}
