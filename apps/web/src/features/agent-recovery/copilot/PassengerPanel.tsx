import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { agentRecoveryApi } from "../agent-recovery.api";
import type { MessageSegment } from "../agent-recovery.types";
import { ErrorLine, SourceBadge } from "./copilot-shared";

const sourceLabel: Record<MessageSegment["source"], string> = {
  default: "Default wording",
  edited: "Edited by you",
  ai: "Rewritten by Gemini",
  rules: "Rewritten by rules",
  sent: "Sent",
};

export function PassengerPanel({
  workflowId,
  editable,
}: {
  workflowId: string;
  editable: boolean;
}) {
  const messages = useQuery({
    queryKey: ["agent-recovery", workflowId, "messages"],
    queryFn: () => agentRecoveryApi.messages(workflowId),
    retry: false,
  });
  if (messages.isLoading)
    return <p className="text-sm text-muted-foreground">Loading passenger groups...</p>;
  if (messages.error || !messages.data) return <ErrorLine error={messages.error} />;

  const { segments, totalRecipients, sent } = messages.data;
  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-emerald-200 bg-emerald-50/60 p-4 text-sm text-emerald-950">
        {sent
          ? `${totalRecipients} bookings were notified. Each group got the wording below.`
          : `${totalRecipients} booking${totalRecipients === 1 ? "" : "s"} will be told once a manager approves. The passenger agent split them into groups so each hears what matters to them. Edit the wording, or ask the agent to rewrite it.`}
      </div>
      {segments.length === 0 && (
        <p className="text-sm text-muted-foreground">No active bookings to notify.</p>
      )}
      <div className="grid gap-3 lg:grid-cols-2">
        {segments.map((segment) => (
          <SegmentCard
            key={`${segment.segment}-${segment.source}-${segment.message}`}
            workflowId={workflowId}
            segment={segment}
            editable={editable && !sent}
          />
        ))}
      </div>
    </div>
  );
}

function SegmentCard({
  workflowId,
  segment,
  editable,
}: {
  workflowId: string;
  segment: MessageSegment;
  editable: boolean;
}) {
  const queryClient = useQueryClient();
  const [text, setText] = useState(segment.message);
  const [instruction, setInstruction] = useState("");
  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: ["agent-recovery", workflowId, "messages"] });
  const save = useMutation({
    mutationFn: () => agentRecoveryApi.saveMessage(workflowId, segment.segment, text),
    onSuccess: refresh,
  });
  const reset = useMutation({
    mutationFn: () => agentRecoveryApi.resetMessage(workflowId, segment.segment),
    onSuccess: refresh,
  });
  const rewrite = useMutation({
    mutationFn: () =>
      agentRecoveryApi.regenerateMessage(workflowId, segment.segment, instruction),
    onSuccess: refresh,
  });
  const busy = save.isPending || reset.isPending || rewrite.isPending;

  return (
    <article className="rounded-lg border border-slate-300 bg-card p-4">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3 className="font-semibold">{segment.label}</h3>
          <p className="text-xs text-muted-foreground">{segment.description}</p>
        </div>
        <div className="text-right">
          <p className="text-lg font-semibold tabular-nums">{segment.recipients}</p>
          <p className="text-[11px] text-muted-foreground">bookings</p>
        </div>
      </div>
      <div className="mt-2 flex flex-wrap gap-2">
        <SourceBadge
          label={sourceLabel[segment.source]}
          tone={segment.source === "ai" ? "good" : segment.source === "sent" ? "info" : "neutral"}
        />
        {segment.source === "sent" && (
          <>
            <SourceBadge label={`${segment.delivered} delivered`} tone="good" />
            {segment.failed > 0 && (
              <SourceBadge label={`${segment.failed} not delivered`} tone="warning" />
            )}
          </>
        )}
      </div>

      {editable ? (
        <textarea
          className="mt-3 min-h-28 w-full rounded-lg border bg-background px-3 py-2 text-sm"
          value={text}
          maxLength={900}
          onChange={(event) => setText(event.target.value)}
        />
      ) : (
        <p className="mt-3 rounded-lg border bg-slate-50 px-3 py-2 text-sm">{segment.message}</p>
      )}
      {segment.warnings.length > 0 && (
        <ul className="mt-2 space-y-1 rounded-md bg-amber-50 p-2 text-xs text-amber-900">
          {segment.warnings.map((warning) => (
            <li key={warning}>⚠ {warning}</li>
          ))}
        </ul>
      )}

      {editable && (
        <div className="mt-3 space-y-2">
          <div className="flex gap-2">
            <input
              className="h-8 min-w-0 flex-1 rounded-md border bg-background px-2 text-sm"
              placeholder='Ask the agent, e.g. "make it shorter"'
              value={instruction}
              maxLength={300}
              onChange={(event) => setInstruction(event.target.value)}
            />
            <Button
              size="sm"
              variant="outline"
              disabled={busy || instruction.trim().length < 3}
              onClick={() => rewrite.mutate()}
            >
              <Sparkles /> Rewrite
            </Button>
          </div>
          <div className="flex gap-2">
            <Button
              size="sm"
              disabled={busy || text.trim() === segment.message}
              onClick={() => save.mutate()}
            >
              Save wording
            </Button>
            {segment.source !== "default" && (
              <Button size="sm" variant="ghost" disabled={busy} onClick={() => reset.mutate()}>
                Back to default
              </Button>
            )}
          </div>
          <ErrorLine error={save.error ?? reset.error ?? rewrite.error} />
        </div>
      )}
    </article>
  );
}
