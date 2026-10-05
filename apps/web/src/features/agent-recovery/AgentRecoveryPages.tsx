import { useCallback, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowRight,
  Bus,
  Check,
  CircleAlert,
  Clock3,
  Network,
  ShieldCheck,
  Ticket,
  UserCheck,
  X,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { ApiError, apiClient } from "@/lib/api/api-client";
import { agentRecoveryApi } from "./agent-recovery.api";
import { RecoveryLiveRun } from "./RecoveryLiveRun";
import type {
  RecoveryIncident,
  RecoveryWorkflowDetail,
  WorkflowStatus,
} from "./agent-recovery.types";

const tone = (status: WorkflowStatus) =>
  status === "Completed"
    ? "good"
    : status === "Failed"
      ? "danger"
      : status === "PausedForApproval"
        ? "warning"
        : "neutral";
const agentTone = (agentName: string) => {
  if (agentName.startsWith("Network")) return "bg-indigo-50/35";
  if (agentName.startsWith("Fleet")) return "bg-sky-50/40";
  if (agentName.startsWith("Dispatch")) return "bg-amber-50/35";
  return "bg-emerald-50/35";
};
const incidentLabel = (incident: RecoveryIncident) => {
  const time = incident.tripTime
    ? new Date(incident.tripTime).toLocaleString([], {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "Centre-wide";
  return `${time} · ${incident.tripRouteNumber ?? "Trip"} · ${incident.title}`;
};
export function AgentRecoveryPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [incidentId, setIncidentId] = useState("");
  const [objective, setObjective] = useState("");
  const [runActive, setRunActive] = useState(false);
  const [startedWorkflowId, setStartedWorkflowId] = useState<string | null>(null);
  const [filter, setFilter] = useState<"all" | WorkflowStatus>("all");
  const [planningIssue, setPlanningIssue] = useState<string | null>(null);
  const workflows = useQuery({
    queryKey: ["agent-recovery", user?.centreId],
    queryFn: () => agentRecoveryApi.list(user?.centreId),
    retry: false,
  });
  const incidents = useQuery({
    queryKey: ["recovery-incidents", user?.centreId],
    refetchInterval: 30_000,
    queryFn: () =>
      apiClient<RecoveryIncident[]>(
        `/api/v1/incidents${user?.centreId ? `?centreId=${user.centreId}` : ""}`,
      ),
    retry: false,
  });
  const start = useMutation({
    mutationFn: (allowFallback: boolean) =>
      agentRecoveryApi.start(incidentId, objective || undefined, allowFallback),
    onSuccess: (workflow) => {
      void queryClient.invalidateQueries({ queryKey: ["agent-recovery"] });
      setStartedWorkflowId(workflow.id);
    },
    onError: (error) => {
      setRunActive(false);
      if (error instanceof ApiError && error.code === "ai_planning_unavailable")
        setPlanningIssue(error.message);
    },
  });
  function beginAssessment(allowFallback: boolean) {
    if (!incidentId) return;
    setPlanningIssue(null);
    setStartedWorkflowId(null);
    setRunActive(true);
    start.mutate(allowFallback);
  }
  const finishRun = useCallback(() => {
    if (startedWorkflowId)
      navigate(`/operations/agent-recovery/${startedWorkflowId}`);
  }, [navigate, startedWorkflowId]);
  const eligibleIncidents = useMemo(
    () =>
      incidents.data?.filter(
        (incident) => incident.tripId && incident.status !== "Resolved",
      ) ?? [],
    [incidents.data],
  );

  if (runActive)
    return (
      <RecoveryLiveRun
        incident={eligibleIncidents.find((item) => item.id === incidentId)}
        workflowId={startedWorkflowId}
        onDone={finishRun}
      />
    );

  const allWorkflows = workflows.data ?? [];
  const awaiting = allWorkflows.filter(
    (workflow) => workflow.status === "PausedForApproval",
  );
  const counts = {
    all: allWorkflows.length,
    PausedForApproval: awaiting.length,
    Running: allWorkflows.filter((item) => item.status === "Running").length,
    Completed: allWorkflows.filter((item) => item.status === "Completed")
      .length,
    Failed: allWorkflows.filter((item) => item.status === "Failed").length,
  };
  const visibleWorkflows = allWorkflows.filter(
    (workflow) => filter === "all" || workflow.status === filter,
  );
  const filters: { value: "all" | WorkflowStatus; label: string }[] = [
    { value: "all", label: "All" },
    { value: "PausedForApproval", label: "Awaiting approval" },
    { value: "Running", label: "Running" },
    { value: "Completed", label: "Completed" },
    { value: "Failed", label: "Failed" },
  ];

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Agent recovery"
        description="Brief the recovery agents on a trip incident. They recommend, a manager decides."
        action={
          <Badge
            variant="outline"
            className="h-8 gap-1.5 border-amber-300 bg-amber-50 px-3 text-amber-900"
          >
            <ShieldCheck className="size-3.5" />
            Approval gate enabled
          </Badge>
        }
      />

      {awaiting.length > 0 && (
        <div className="flex flex-col gap-3 rounded-lg border border-amber-300 bg-amber-50 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-3">
            <CircleAlert className="mt-0.5 size-5 shrink-0 text-amber-700" />
            <div>
              <p className="font-medium text-amber-950">
                {awaiting.length} recovery proposal
                {awaiting.length === 1 ? " is" : "s are"} waiting for your
                decision
              </p>
              <p className="text-sm text-amber-800">
                No trip changes are applied until a manager approves.
              </p>
            </div>
          </div>
          <Button
            className="shrink-0 bg-amber-600 text-white hover:bg-amber-700"
            render={
              <Link to={`/operations/agent-recovery/${awaiting[0].id}`} />
            }
          >
            Review proposal <ArrowRight />
          </Button>
        </div>
      )}

      <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_340px]">
        <form
          className="rounded-lg border bg-card"
          onSubmit={(event) => {
            event.preventDefault();
            beginAssessment(false);
          }}
        >
          <header className="flex items-center gap-3 border-b px-5 py-4">
            <div>
              <h2 className="font-semibold">Brief the agents</h2>
              <p className="text-sm text-muted-foreground">
                Choose an open incident and say what a good outcome looks like.
              </p>
            </div>
          </header>
          <div className="grid gap-4 p-5">
            <label className="grid gap-1.5 text-sm font-medium">
              Incident
              <Select
                value={incidentId || undefined}
                onValueChange={(value) => setIncidentId(String(value ?? ""))}
                itemToStringLabel={(value) => {
                  const incident = eligibleIncidents.find(
                    (item) => item.id === value,
                  );
                  return incident
                    ? incidentLabel(incident)
                    : "Select an open trip incident";
                }}
              >
                <SelectTrigger className="w-full bg-muted/60">
                  <SelectValue placeholder="Select an open trip incident" />
                </SelectTrigger>
                <SelectContent>
                  {eligibleIncidents.map((incident) => (
                    <SelectItem key={incident.id} value={incident.id}>
                      <span className="flex min-w-0 flex-col py-0.5">
                        <span className="font-medium">
                          {incidentLabel(incident)}
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {incident.type} · {incident.severity} severity
                        </span>
                      </span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>

            <label className="grid gap-1.5 text-sm font-medium">
              <span>
                Recovery objective{" "}
                <span className="font-normal text-muted-foreground">
                  (optional)
                </span>
              </span>
              <Textarea
                className="bg-muted/60"
                rows={3}
                value={objective}
                onChange={(event) => setObjective(event.target.value)}
                placeholder="e.g. Restore service with a capacity-safe replacement. Leave blank to let the agents derive it from the incident."
              />
            </label>

            {start.error && (
              <p className="text-sm text-destructive">
                {start.error instanceof Error
                  ? start.error.message
                  : "Could not start the recovery workflow."}
              </p>
            )}

            {!incidents.isLoading && !eligibleIncidents.length ? (
              <div className="flex flex-col gap-3 rounded-lg border bg-muted/40 p-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-medium">No open trip incidents</p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Report an incident from Dispatch before running recovery.
                  </p>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  className="shrink-0"
                  render={<Link to="/operations/incidents/new" />}
                >
                  Report incident
                </Button>
              </div>
            ) : (
              <div className="flex justify-end">
                <Button
                  type="submit"
                  className="h-10 px-5"
                  disabled={!incidentId || start.isPending}
                >
                  {start.isPending ? "Starting..." : "Start assessment"}
                </Button>
              </div>
            )}
          </div>
        </form>

        <aside className="self-start rounded-lg border bg-card p-5">
          <h2 className="font-semibold">Your recovery team</h2>
          <ol className="mt-4 space-y-3">
            {[
              {
                icon: Network,
                name: "Network Continuity",
                role: "Bay and revised departure time",
                accent: "bg-indigo-50 text-indigo-700",
              },
              {
                icon: Bus,
                name: "Fleet Readiness",
                role: "Replacement vehicles and capacity",
                accent: "bg-sky-50 text-sky-700",
              },
              {
                icon: UserCheck,
                name: "Dispatch Recovery",
                role: "Eligible drivers and service window",
                accent: "bg-amber-50 text-amber-700",
              },
              {
                icon: Ticket,
                name: "Passenger & Fare Impact",
                role: "Affected bookings and fares",
                accent: "bg-rose-50 text-rose-700",
              },
            ].map(({ icon: Icon, name, role, accent }) => (
              <li key={name} className="flex items-center gap-3">
                <span
                  className={`flex size-9 shrink-0 items-center justify-center rounded-lg ${accent}`}
                >
                  <Icon className="size-4" />
                </span>
                <div className="min-w-0">
                  <p className="text-sm font-medium">{name}</p>
                  <p className="truncate text-xs text-muted-foreground">
                    {role}
                  </p>
                </div>
              </li>
            ))}
          </ol>
          <div className="mt-4 flex items-start gap-2.5 rounded-md bg-muted/50 p-3 text-xs leading-5 text-muted-foreground">
            <ShieldCheck className="mt-0.5 size-4 shrink-0 text-primary" />
            <p>Trips only change after a manager approves.</p>
          </div>
        </aside>
      </section>

      <section className="overflow-hidden rounded-lg border bg-card">
        <header className="flex flex-col gap-3 border-b px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold">Recovery workflows</h2>
            <p className="text-sm text-muted-foreground">
              Every assessment, decision and executed recovery is kept here.
            </p>
          </div>
          <div className="flex flex-wrap gap-1.5">
            {filters.map((item) => (
              <Button
                key={item.value}
                size="sm"
                variant={filter === item.value ? "default" : "outline"}
                onClick={() => setFilter(item.value)}
              >
                {item.label}
                <span className="tabular-nums opacity-70">
                  {counts[item.value]}
                </span>
              </Button>
            ))}
          </div>
        </header>
        {workflows.isLoading ? (
          <p className="p-6 text-sm text-muted-foreground">
            Loading workflows...
          </p>
        ) : workflows.error ? (
          <p className="p-6 text-sm text-destructive">
            Could not load recovery workflows.
          </p>
        ) : !allWorkflows.length ? (
          <div className="p-10 text-center">
            <p className="font-medium">No recovery workflows yet</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Brief the agents on an open trip incident to get a supervised
              recovery proposal.
            </p>
          </div>
        ) : !visibleWorkflows.length ? (
          <p className="p-8 text-center text-sm text-muted-foreground">
            No workflows with this status.
          </p>
        ) : (
          <div className="divide-y">
            {visibleWorkflows.map((workflow) => (
              <Link
                key={workflow.id}
                to={`/operations/agent-recovery/${workflow.id}`}
                className="grid gap-3 px-5 py-4 transition-colors hover:bg-muted/30 md:grid-cols-[minmax(0,1fr)_150px_110px] md:items-center"
              >
                <div className="min-w-0">
                  <p className="truncate font-medium">
                    Route {workflow.trip.route} · {workflow.incident.title}
                  </p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {workflow.incident.type} · {workflow.incident.severity}{" "}
                    severity · started{" "}
                    {new Date(workflow.createdAt).toLocaleString([], {
                      month: "short",
                      day: "numeric",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </p>
                </div>
                <StatusBadge
                  label={
                    workflow.status === "PausedForApproval"
                      ? "Awaiting approval"
                      : workflow.status
                  }
                  tone={tone(workflow.status)}
                />
                <span className="inline-flex items-center gap-1 text-sm font-medium text-primary md:justify-end">
                  Open <ArrowRight className="size-4" />
                </span>
              </Link>
            ))}
          </div>
        )}
      </section>
      <AlertDialog
        open={planningIssue !== null}
        onOpenChange={(open) => {
          if (!open) setPlanningIssue(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>AI planning is unavailable</AlertDialogTitle>
            <AlertDialogDescription>
              The AI planner could not create a plan right now. This is usually
              temporary, for example when the model is busy. You can try again,
              or continue with the built-in safe plan. The safe plan runs the
              same four specialists and is recorded as a fallback on the
              workflow.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {planningIssue && (
            <p className="rounded-md bg-muted/50 p-3 text-xs text-muted-foreground">
              {planningIssue}
            </p>
          )}
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="outline"
              onClick={() => beginAssessment(false)}
            >
              Try again
            </AlertDialogAction>
            <AlertDialogAction onClick={() => beginAssessment(true)}>
              Use safe plan
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}

export function AgentRecoveryDetailPage() {
  const { workflowId = "" } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [note, setNote] = useState("");
  const workflow = useQuery({
    queryKey: ["agent-recovery", workflowId],
    queryFn: () => agentRecoveryApi.detail(workflowId),
    enabled: !!workflowId,
    retry: false,
  });
  const decide = useMutation({
    mutationFn: (decision: "Approved" | "Rejected") =>
      agentRecoveryApi.decide(workflowId, decision, note || undefined),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["agent-recovery"] });
    },
  });
  if (workflow.isLoading)
    return (
      <main className="flex flex-1 items-center justify-center bg-muted/20 p-4 text-sm text-muted-foreground">
        Loading recovery workflow...
      </main>
    );
  if (workflow.error || !workflow.data)
    return (
      <main className="flex flex-1 flex-col items-center justify-center gap-3 bg-muted/20 p-4">
        <CircleAlert className="size-7 text-destructive" />
        <p>Recovery workflow not found.</p>
        <Button
          variant="outline"
          onClick={() => navigate("/operations/agent-recovery")}
        >
          Back to recovery agents
        </Button>
      </main>
    );
  return (
    <RecoveryDetail
      workflow={workflow.data}
      note={note}
      onNoteChange={setNote}
      pending={decide.isPending}
      canApprove={user?.role === "Admin" || user?.role === "CentreManager"}
      onDecision={(decision) => decide.mutate(decision)}
      error={decide.error instanceof Error ? decide.error.message : ""}
    />
  );
}

function RecoveryDetail({
  workflow,
  note,
  onNoteChange,
  pending,
  canApprove,
  onDecision,
  error,
}: {
  workflow: RecoveryWorkflowDetail;
  note: string;
  onNoteChange: (value: string) => void;
  pending: boolean;
  canApprove: boolean;
  onDecision: (decision: "Approved" | "Rejected") => void;
  error: string;
}) {
  const { summary, trip, plan, validationResults, steps, approvals } = workflow;
  const approval = approvals[0];
  const isPending =
    summary.status === "PausedForApproval" && approval?.decision === "Pending";
  return (
    <main className="flex flex-1 flex-col gap-5 bg-slate-50/70 p-4">
      <PageHeading
        title="Recovery workflow"
        description={`${trip.route} · ${summary.incident.title}`}
        action={
          <Button
            variant="outline"
            render={<Link to="/operations/agent-recovery" />}
          >
            <ArrowLeft /> Recovery queue
          </Button>
        }
      />
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.55fr)_minmax(330px,0.75fr)]">
        <article className="overflow-hidden rounded-xl border border-slate-300 bg-card">
          <div className="flex flex-wrap items-start justify-between gap-3 border-b border-primary/20 bg-primary/[0.035] px-5 py-5">
            <div>
              <p className="text-xs font-medium text-muted-foreground">
                WORKFLOW OBJECTIVE
              </p>
              <h2 className="mt-1 text-lg font-semibold">
                {summary.objective}
              </h2>
            </div>
            <StatusBadge
              label={
                summary.status === "PausedForApproval"
                  ? "Awaiting approval"
                  : summary.status
              }
              tone={tone(summary.status)}
            />
          </div>
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-5 py-3 text-sm">
            {workflow.planning.planningMode.startsWith("Gemini") ? (
              <>
                <StatusBadge label="Planned by Gemini" tone="good" />
                <span className="text-muted-foreground">
                  {workflow.planning.modelName} ·{" "}
                  {workflow.planning.totalTokenCount.toLocaleString()} tokens ·{" "}
                  {(workflow.planning.planningDurationMs / 1000).toFixed(1)}s
                </span>
              </>
            ) : (
              <>
                <StatusBadge label="Fallback plan" tone="warning" />
                <span className="text-muted-foreground">
                  {workflow.planning.planningFallbackReason ??
                    "A safe built-in plan was used instead of an AI plan."}
                </span>
              </>
            )}
          </div>
          <div className="grid gap-3 border-b border-slate-300 bg-slate-50 px-5 py-4 sm:grid-cols-3">
            <Fact
              label="Current departure"
              value={new Date(trip.scheduledTime).toLocaleString()}
            />
            <Fact
              label="Current assignment"
              value={`${trip.vehicle} · ${trip.driver}`}
            />
            <Fact label="Current bay" value={trip.bay} />
          </div>
          {summary.failureReason && (
            <div className="mx-5 mt-5 rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive">
              <p className="font-medium">Safe failure</p>
              <p className="mt-1">{summary.failureReason}</p>
            </div>
          )}
          <div className="border-b border-slate-300 px-5 py-5">
            <div className="flex items-center justify-between gap-3">
              <h2 className="font-semibold">Execution plan</h2>
              <span className="text-xs text-muted-foreground">
                Persisted with this workflow
              </span>
            </div>
            {plan.length ? (
              <ol className="mt-3 divide-y divide-indigo-100 rounded-lg border border-indigo-200 bg-indigo-50/35">
                {plan.map((planStep) => (
                  <li
                    key={planStep.order}
                    className="flex gap-3 px-4 py-3 text-sm"
                  >
                    <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-xs font-semibold text-indigo-800">
                      {planStep.order}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="font-medium">{planStep.title}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">
                        {planStep.owner} · {planStep.purpose}
                      </p>
                    </div>
                    <span className="shrink-0 text-xs font-medium text-indigo-700">
                      {planStep.status}
                    </span>
                  </li>
                ))}
              </ol>
            ) : (
              <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-950">
                This workflow was completed before persisted execution plans
                were introduced. Its agent recommendations and manager decision
                remain available below.
              </div>
            )}
          </div>
          <div className="px-5 py-5">
            <h2 className="font-semibold">Agent recommendations</h2>
            <div className="mt-3 space-y-3">
              {steps.map((step) => (
                <article
                  key={step.id}
                  className={`rounded-lg border border-slate-300 p-4 ${agentTone(step.agentName)}`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="font-medium">{step.agentName}</h3>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {step.output?.summary ??
                          step.error ??
                          "No recommendation returned."}
                      </p>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {step.durationMs} ms
                    </span>
                  </div>
                  {step.output?.reasons?.length ? (
                    <ul className="mt-3 space-y-1 text-sm text-muted-foreground">
                      {step.output.reasons.map((reason) => (
                        <li key={reason}>• {reason}</li>
                      ))}
                    </ul>
                  ) : null}
                  {step.output?.warnings?.length ? (
                    <div className="mt-3 rounded-md bg-amber-50 p-3 text-sm text-amber-900">
                      {step.output.warnings.map((warning) => (
                        <p key={warning}>{warning}</p>
                      ))}
                    </div>
                  ) : null}
                  <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 border-t border-slate-200 pt-3 text-xs text-muted-foreground">
                    <span>
                      {step.toolCalls.length
                        ? `${step.toolCalls.length} allow-listed tool call${step.toolCalls.length === 1 ? "" : "s"}`
                        : "Legacy recommendation record"}
                    </span>
                    <span>
                      {step.retryCount === 0
                        ? "Completed on first attempt"
                        : `${step.retryCount} retry used`}
                    </span>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </article>
        <aside className="space-y-4">
          <article className="rounded-xl border border-emerald-200 bg-emerald-50/35 p-5">
            <h2 className="font-semibold">Deterministic validation</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Safety checks are recorded before approval and repeated when an
              approved recovery is applied.
            </p>
            <div className="mt-3 space-y-2">
              {validationResults.length ? (
                validationResults.map((result, index) => (
                  <div
                    key={`${result.phase}-${result.check}-${index}`}
                    className={`flex gap-2 rounded-md border px-3 py-2 text-sm ${
                      result.passed
                        ? "border-emerald-200 bg-white/80"
                        : "border-red-200 bg-red-50"
                    }`}
                  >
                    {result.passed ? (
                      <Check className="mt-0.5 size-4 shrink-0 text-emerald-700" />
                    ) : (
                      <CircleAlert className="mt-0.5 size-4 shrink-0 text-destructive" />
                    )}
                    <div>
                      <p className="font-medium">{result.check}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">
                        {result.phase} · {result.detail}
                      </p>
                    </div>
                  </div>
                ))
              ) : (
                <p className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950">
                  {summary.status === "Completed"
                    ? "This workflow predates detailed validation records. It completed under the earlier recovery safety checks."
                    : "Validation did not run because the required agent proposal was incomplete."}
                </p>
              )}
            </div>
          </article>
          <article className="rounded-xl border border-slate-300 bg-card p-5">
            <h2 className="font-semibold">Manager decision</h2>
            <p className="mt-2 text-sm text-muted-foreground">
              {approval?.reason ?? "No approval request was created."}
            </p>
            {isPending && canApprove ? (
              <>
                <label className="mt-4 grid gap-1.5 text-sm font-medium">
                  Decision note{" "}
                  <span className="font-normal text-muted-foreground">
                    (optional)
                  </span>
                  <textarea
                    className="min-h-24 rounded-lg border bg-background px-3 py-2 text-sm"
                    value={note}
                    onChange={(event) => onNoteChange(event.target.value)}
                    placeholder="Record why this recovery is approved or rejected"
                  />
                </label>
                <div className="mt-4 grid gap-2">
                  <Button
                    disabled={pending}
                    onClick={() => onDecision("Approved")}
                  >
                    <Check /> Approve and apply recovery
                  </Button>
                  <Button
                    variant="outline"
                    disabled={pending}
                    onClick={() => onDecision("Rejected")}
                  >
                    <X /> Reject proposal
                  </Button>
                </div>
                {error && (
                  <p className="mt-3 text-sm text-destructive">{error}</p>
                )}
              </>
            ) : isPending ? (
              <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950">
                This workflow is waiting for a Centre Manager or Admin approval.
              </div>
            ) : (
              <div
                className={`mt-4 rounded-lg border p-3 text-sm ${
                  approval?.decision === "Approved"
                    ? "border-emerald-200 bg-emerald-50"
                    : "border-slate-200 bg-slate-50"
                }`}
              >
                <p className="font-medium">
                  {approval?.decision ?? "No decision"}
                </p>
                {approval?.decisionNote && (
                  <p className="mt-1 text-muted-foreground">
                    {approval.decisionNote}
                  </p>
                )}
                {approval?.reviewedBy && (
                  <p className="mt-1 text-muted-foreground">
                    Reviewed by {approval.reviewedBy}
                  </p>
                )}
                {approval?.appliedAt && (
                  <p className="mt-1 text-emerald-700">
                    Recovery applied{" "}
                    {new Date(approval.appliedAt).toLocaleString()}
                  </p>
                )}
              </div>
            )}
          </article>
          <article className="rounded-xl border border-indigo-200 bg-indigo-50/35 p-5">
            <h2 className="font-semibold">Safety controls</h2>
            <ul className="mt-3 space-y-2 text-sm text-muted-foreground">
              <li className="flex gap-2">
                <ShieldCheck className="size-4 shrink-0 text-primary" />
                Agents create recommendations only.
              </li>
              <li className="flex gap-2">
                <ShieldCheck className="size-4 shrink-0 text-primary" />
                Vehicle, driver, bay, conflict and capacity checks run again at
                approval.
              </li>
              <li className="flex gap-2">
                <ShieldCheck className="size-4 shrink-0 text-primary" />
                Only an approved workflow can change the trip.
              </li>
            </ul>
          </article>
          <article className="rounded-xl border border-slate-300 bg-slate-50 p-5">
            <h2 className="font-semibold">Audit trail</h2>
            <p className="mt-2 flex items-center gap-2 text-sm text-muted-foreground">
              <Clock3 className="size-4" />
              Created {new Date(summary.createdAt).toLocaleString()}
            </p>
            <p className="mt-2 text-sm text-muted-foreground">
              {steps.length} agent step{steps.length === 1 ? "" : "s"} retained
              with inputs, outputs, tool calls, retries and timings.
            </p>
          </article>
        </aside>
      </section>
    </main>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-sm font-medium">{value}</p>
    </div>
  );
}
