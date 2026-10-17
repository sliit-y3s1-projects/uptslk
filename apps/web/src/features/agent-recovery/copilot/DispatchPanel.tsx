import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Brain, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { agentRecoveryApi } from "../agent-recovery.api";
import { ErrorLine, SourceBadge } from "./copilot-shared";

export function DispatchPanel({
  workflowId,
  canRevise,
  pending,
}: {
  workflowId: string;
  canRevise: boolean;
  pending: boolean;
}) {
  const queryClient = useQueryClient();
  const lessons = useQuery({
    queryKey: ["agent-recovery", workflowId, "lessons"],
    queryFn: () => agentRecoveryApi.lessons(workflowId),
    retry: false,
  });
  const [reason, setReason] = useState("");
  const [avoidVehicle, setAvoidVehicle] = useState(false);
  const [avoidDriver, setAvoidDriver] = useState(false);
  const [avoidBay, setAvoidBay] = useState(false);
  const [maxDelay, setMaxDelay] = useState("");
  const revise = useMutation({
    mutationFn: () =>
      agentRecoveryApi.revise(workflowId, {
        reason,
        avoidVehicle,
        avoidDriver,
        avoidBay,
        maxDelayMinutes: maxDelay === "" ? null : Number(maxDelay),
      }),
    onSuccess: () => {
      setReason("");
      setAvoidVehicle(false);
      setAvoidDriver(false);
      setAvoidBay(false);
      setMaxDelay("");
      void queryClient.invalidateQueries({ queryKey: ["agent-recovery"] });
    },
  });

  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <section id="reject-and-revise" className="rounded-lg border border-amber-200 bg-amber-50/40 p-4">
        <h3 className="flex items-center gap-2 font-semibold">
          <RotateCcw className="size-4" /> Reject and ask the agent to revise
        </h3>
        <p className="mt-1 text-sm text-muted-foreground">
          Say what is wrong. The dispatch agent keeps your constraints, searches again and puts a
          new proposal up for approval. Your decision is remembered for future recoveries.
        </p>
        {pending && canRevise ? (
          <div className="mt-3 space-y-3">
            <textarea
              className="min-h-24 w-full rounded-lg border bg-background px-3 py-2 text-sm"
              placeholder='e.g. "This driver already worked a double shift" or "30 minutes is too long"'
              value={reason}
              maxLength={2000}
              onChange={(event) => setReason(event.target.value)}
            />
            <div className="space-y-2 text-sm">
              <p className="text-xs text-muted-foreground">
                Optional. Leave all empty and the agent works it out from your reason.
              </p>
              <label className="flex items-center gap-2">
                <Checkbox checked={avoidVehicle} onCheckedChange={(value) => setAvoidVehicle(value === true)} />
                Do not use this bus
              </label>
              <label className="flex items-center gap-2">
                <Checkbox checked={avoidDriver} onCheckedChange={(value) => setAvoidDriver(value === true)} />
                Do not use this driver
              </label>
              <label className="flex items-center gap-2">
                <Checkbox checked={avoidBay} onCheckedChange={(value) => setAvoidBay(value === true)} />
                Do not use this bay
              </label>
              <label className="flex items-center gap-2">
                Delay at most
                <input
                  type="number"
                  min={0}
                  max={120}
                  className="h-8 w-20 rounded-md border bg-background px-2 text-sm"
                  value={maxDelay}
                  onChange={(event) => setMaxDelay(event.target.value)}
                />
                min
              </label>
            </div>
            <Button disabled={revise.isPending || reason.trim().length < 3} onClick={() => revise.mutate()}>
              Reject and revise
            </Button>
            <ErrorLine error={revise.error} />
          </div>
        ) : (
          <p className="mt-3 text-sm text-muted-foreground">
            {pending
              ? "Only a Centre Manager or Admin can reject a proposal."
              : "This workflow is no longer waiting for a decision."}
          </p>
        )}
        {revise.data && (
          <div className="mt-3 rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm">
            <p className="font-medium">
              {revise.data.status === "Failed"
                ? "No revision was possible"
                : `Revision #${revise.data.revision} is ready for approval`}
            </p>
            <p className="mt-1">{revise.data.rationale}</p>
            {revise.data.appliedConstraints.length > 0 && (
              <p className="mt-1 text-xs text-muted-foreground">
                Constraints: {revise.data.appliedConstraints.join(" · ")}. Considered{" "}
                {revise.data.alternativesConsidered} options.
              </p>
            )}
          </div>
        )}
      </section>

      <section className="rounded-lg border border-violet-200 bg-violet-50/40 p-4">
        <h3 className="flex items-center gap-2 font-semibold">
          <Brain className="size-4" /> What the agents remember
        </h3>
        <p className="mt-1 text-sm text-muted-foreground">
          Earlier manager decisions for this route and incident type. They lower the score of
          rejected options and appear in the planner prompt.
        </p>
        {lessons.isLoading && <p className="mt-3 text-sm text-muted-foreground">Loading...</p>}
        {lessons.data?.length === 0 && (
          <p className="mt-3 text-sm text-muted-foreground">
            Nothing yet. The first approval or rejection starts the memory.
          </p>
        )}
        <ul className="mt-3 space-y-2">
          {lessons.data?.map((lesson) => (
            <li key={lesson.id} className="rounded-md border bg-white/80 p-3 text-sm">
              <div className="flex items-center justify-between gap-2">
                <SourceBadge
                  label={lesson.kind}
                  tone={lesson.kind === "Approved" ? "good" : "warning"}
                />
                <span className="text-xs text-muted-foreground">
                  {new Date(lesson.createdAt).toLocaleDateString()}
                </span>
              </div>
              <p className="mt-1">{lesson.summary}</p>
              {lesson.reason && (
                <p className="mt-1 text-xs text-muted-foreground">“{lesson.reason}”</p>
              )}
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
