import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  Bot,
  Check,
  CircleAlert,
  Clock3,
  LoaderCircle,
  ShieldCheck,
  X,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
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
import { apiClient } from "@/lib/api/api-client";
import { agentRecoveryApi } from "./agent-recovery.api";
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
  if (agentName.startsWith("Network"))
    return "bg-indigo-50/35";
  if (agentName.startsWith("Fleet"))
    return "bg-sky-50/40";
  if (agentName.startsWith("Dispatch"))
    return "bg-amber-50/35";
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
const assessmentStages = [
  {
    title: "Assess service continuity",
    owner: "Network Continuity Agent",
    detail: "Reviewing the departure bay and a safe revised departure time.",
  },
  {
    title: "Assess fleet readiness",
    owner: "Fleet Readiness Agent",
    detail: "Checking active replacement vehicles, maintenance and capacity.",
  },
  {
    title: "Assess dispatch availability",
    owner: "Dispatch Recovery Agent",
    detail: "Checking eligible drivers against the proposed service window.",
  },
  {
    title: "Assess passenger impact",
    owner: "Passenger & Fare Impact Agent",
    detail: "Counting affected bookings and reviewing fare implications.",
  },
  {
    title: "Run safety validation",
    owner: "Deterministic validation",
    detail: "Validating vehicle, driver, bay, capacity and conflict rules.",
  },
  {
    title: "Prepare approval-ready audit",
    owner: "Workflow coordinator",
    detail: "Persisting the plan, recommendations, tool calls and safety checks.",
  },
];
const wait = (milliseconds: number) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds));

export function AgentRecoveryPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [incidentId, setIncidentId] = useState("");
  const [objective, setObjective] = useState("");
  const [assessmentStage, setAssessmentStage] = useState<number | null>(null);
  const workflows = useQuery({
    queryKey: ["agent-recovery", user?.centreId],
    queryFn: () => agentRecoveryApi.list(user?.centreId),
    retry: false,
  });
  const incidents = useQuery({
    queryKey: ["recovery-incidents", user?.centreId],
    queryFn: () =>
      apiClient<RecoveryIncident[]>(
        `/api/v1/incidents${user?.centreId ? `?centreId=${user.centreId}` : ""}`,
      ),
    retry: false,
  });
  const start = useMutation({
    mutationFn: async () => {
      const workflowRequest = agentRecoveryApi.start(
        incidentId,
        objective || undefined,
      );
      const [workflow] = await Promise.all([workflowRequest, wait(6000)]);
      return workflow;
    },
    onSuccess: (workflow) => {
      void queryClient.invalidateQueries({ queryKey: ["agent-recovery"] });
      navigate(`/operations/agent-recovery/${workflow.id}`);
    },
    onError: () => setAssessmentStage(null),
  });
  const eligibleIncidents = useMemo(
    () =>
      incidents.data?.filter(
        (incident) => incident.tripId && incident.status !== "Resolved",
      ) ?? [],
    [incidents.data],
  );

  if (start.isPending && assessmentStage !== null)
    return <RecoveryAssessmentRun activeStage={assessmentStage} />;

  return (
    <main className="flex flex-1 flex-col gap-5 bg-slate-50/70 p-4">
      <PageHeading
        title="Agent recovery"
        description="Run a supervised recovery assessment for a trip incident. Agents recommend; an authorized manager decides."
      />
      <section className="overflow-hidden rounded-xl border border-slate-300 bg-card">
        <header className="flex flex-wrap items-start justify-between gap-3 border-b border-primary/20 bg-primary/[0.035] px-5 py-4 sm:px-6">
          <div>
            <h2 className="font-semibold">New recovery assessment</h2>
            <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
              Select an incident and define the outcome the recovery team should
              work toward.
            </p>
          </div>
          <Badge
            variant="outline"
            className="h-7 gap-1.5 border-amber-300 bg-amber-50 px-3 text-amber-900"
          >
            <ShieldCheck className="size-3.5" />
            Approval gate enabled
          </Badge>
        </header>

        <div className="grid lg:grid-cols-[280px_minmax(0,1fr)]">
          <aside className="border-b border-primary/15 bg-primary/[0.045] px-5 py-5 lg:border-r lg:border-b-0 sm:px-6">
            <p className="text-xs font-semibold uppercase tracking-[0.1em] text-primary">
              What happens next
            </p>
            <ol className="mt-4 space-y-4">
              {[
                {
                  number: "1",
                  title: "Specialists assess",
                  detail: "Network, fleet, dispatch and passengers",
                },
                {
                  number: "2",
                  title: "Safety rules validate",
                  detail: "Capacity, availability and conflicts",
                },
                {
                  number: "3",
                  title: "Manager decides",
                  detail: "No trip changes before approval",
                },
              ].map(({ number, title, detail }) => (
                <li key={number} className="flex gap-3">
                  <span
                    className="flex size-7 shrink-0 items-center justify-center rounded-full border border-primary bg-primary text-xs font-semibold text-primary-foreground"
                  >
                    {number}
                  </span>
                  <div>
                    <p className="text-sm font-medium">{title}</p>
                    <p className="mt-0.5 text-xs leading-5 text-muted-foreground">
                      {detail}
                    </p>
                  </div>
                </li>
              ))}
            </ol>
          </aside>

          <form
            className="grid content-start gap-4 bg-white px-5 py-5 sm:px-6"
            onSubmit={(event) => {
              event.preventDefault();
              if (!incidentId) return;
              setAssessmentStage(0);
              assessmentStages.forEach((_, index) => {
                window.setTimeout(
                  () => setAssessmentStage(index + 1),
                  (index + 1) * 850,
                );
              });
              start.mutate();
            }}
          >
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
                <SelectTrigger className="h-11 w-full border-slate-300 bg-white focus-visible:border-primary">
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
              <Input
                className="h-11 border-slate-300 bg-white focus-visible:border-primary"
                value={objective}
                onChange={(event) => setObjective(event.target.value)}
                placeholder="Restore service with a capacity-safe replacement"
              />
              <span className="text-xs font-normal text-muted-foreground">
                Leave blank to use the incident-based objective automatically.
              </span>
            </label>

            {start.error && (
              <p className="text-sm text-destructive">
                {start.error instanceof Error
                  ? start.error.message
                  : "Could not start the recovery workflow."}
              </p>
            )}

            {!incidents.isLoading && !eligibleIncidents.length ? (
              <div className="flex flex-col gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-medium text-amber-950">
                    No open trip incidents
                  </p>
                  <p className="mt-1 text-xs text-amber-800">
                    Report an incident from Dispatch before running recovery.
                  </p>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  className="shrink-0 border-amber-300 bg-white"
                  render={<Link to="/operations/incidents/new" />}
                >
                  Report incident
                </Button>
              </div>
            ) : (
              <div className="flex justify-end border-t border-slate-200 pt-4">
                <Button
                  type="submit"
                  className="h-10 px-5"
                  disabled={!incidentId || start.isPending}
                >
                  {start.isPending ? "Starting assessment..." : "Start assessment"}
                </Button>
              </div>
            )}
          </form>
        </div>
      </section>
      <section className="overflow-hidden rounded-xl border border-slate-300 bg-card">
        <header className="flex items-center justify-between border-b border-slate-300 bg-slate-50 px-5 py-4">
          <div>
            <h2 className="font-semibold">Recovery workflows</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Every assessment, decision, and executed recovery is retained
              here.
            </p>
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
        ) : !workflows.data?.length ? (
          <div className="p-10 text-center">
            <Bot className="mx-auto size-7 text-muted-foreground" />
            <p className="mt-3 font-medium">No recovery workflows yet</p>
            <p className="mt-1 text-sm text-muted-foreground">
              Start from an open trip incident when you need a supervised
              recovery proposal.
            </p>
          </div>
        ) : (
          <div className="divide-y">
            {workflows.data.map((workflow) => (
              <Link
                key={workflow.id}
                to={`/operations/agent-recovery/${workflow.id}`}
                className="grid gap-3 px-5 py-4 transition-colors hover:bg-muted/30 md:grid-cols-[minmax(0,1fr)_170px_150px] md:items-center"
              >
                <div>
                  <p className="font-medium">
                    {workflow.trip.route} · {workflow.incident.title}
                  </p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {new Date(workflow.createdAt).toLocaleString()} ·{" "}
                    {workflow.incident.type} incident
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
                <span className="text-sm text-primary md:text-right">
                  Open workflow →
                </span>
              </Link>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}

function RecoveryAssessmentRun({ activeStage }: { activeStage: number }) {
  const completedCount = Math.min(activeStage, assessmentStages.length);
  const active = assessmentStages[activeStage];
  const progress = Math.round(
    (completedCount / assessmentStages.length) * 100,
  );

  return (
    <main className="flex flex-1 justify-center bg-background px-5 py-10 sm:px-8 lg:py-14">
      <section className="recovery-workspace-enter w-full max-w-5xl">
        <header className="max-w-3xl">
          <div className="flex flex-wrap items-center gap-3">
            <Badge variant="secondary" className="h-7 gap-2 px-3 text-primary">
              <LoaderCircle className="size-3.5 animate-spin" />
              Agents working
            </Badge>
            <span className="text-xs text-muted-foreground">
              Recommendations only · no trip changes applied
            </span>
          </div>
          <h1 className="mt-5 text-3xl font-semibold tracking-tight text-foreground">
            Preparing a recovery proposal
          </h1>
          <p className="mt-2 max-w-2xl text-base leading-7 text-muted-foreground">
            Four transport specialists are reviewing the incident, then the
            coordinator will validate and assemble the evidence for approval.
          </p>
          <div className="mt-6 max-w-2xl">
            <Progress value={progress} className="recovery-progress" />
            <div className="mt-2 flex items-center justify-between text-xs text-muted-foreground">
              <span>
                Step {Math.min(activeStage + 1, assessmentStages.length)} of {assessmentStages.length}
              </span>
              <span className="tabular-nums">{progress}% complete</span>
            </div>
          </div>
        </header>

        <Separator className="my-9" />

        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_280px] lg:gap-14">
          <div>
            <div className="mb-5 flex items-center justify-between gap-3">
              <div>
                <h2 className="text-sm font-semibold">Live activity</h2>
                <p className="mt-1 text-xs text-muted-foreground">
                  Each result is persisted before the next specialist begins.
                </p>
              </div>
              <span className="text-xs text-muted-foreground">
                {completedCount} recorded
              </span>
            </div>
            <ol aria-label="Recovery assessment progress">
            {assessmentStages.map((stage, index) => {
              const complete = index < activeStage;
              const isActive = index === activeStage;
              return (
                <li
                  key={stage.title}
                  aria-current={isActive ? "step" : undefined}
                  className={`relative grid grid-cols-[32px_minmax(0,1fr)_auto] gap-4 pb-7 last:pb-0 ${
                    complete
                      ? "recovery-step-complete"
                      : isActive
                        ? "recovery-step-active"
                        : ""
                  }`}
                >
                  {index < assessmentStages.length - 1 && (
                    <span
                      aria-hidden="true"
                      className={`recovery-step-rail absolute left-[15px] top-8 h-[calc(100%-1.25rem)] w-px ${
                        complete ? "bg-emerald-500/50" : "bg-border"
                      }`}
                    />
                  )}
                  <span
                    className={`recovery-step-icon relative z-10 flex size-8 items-center justify-center rounded-full border text-xs font-semibold ${
                      complete
                        ? "border-emerald-600 bg-emerald-600 text-white"
                        : isActive
                          ? "border-primary bg-primary/5 text-primary ring-4 ring-primary/10"
                          : "border-border bg-background text-muted-foreground"
                    }`}
                  >
                    {complete ? (
                      <Check className="size-3.5" />
                    ) : isActive ? (
                      <LoaderCircle className="size-3.5 animate-spin" />
                    ) : (
                      index + 1
                    )}
                  </span>
                  <div className="min-w-0 pt-1">
                    <p className={`text-sm ${complete || isActive ? "font-medium text-foreground" : "text-muted-foreground"}`}>
                      {stage.title}
                    </p>
                    {isActive && (
                      <div className="recovery-step-detail mt-2 max-w-xl">
                        <p className="text-sm leading-6 text-muted-foreground">
                          {stage.detail}
                        </p>
                        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs font-medium text-primary">
                          <span>{stage.owner}</span>
                          <span aria-hidden="true" className="flex gap-0.5">
                            <span className="recovery-thinking-dot size-1 rounded-full bg-primary" />
                            <span className="recovery-thinking-dot size-1 rounded-full bg-primary [animation-delay:140ms]" />
                            <span className="recovery-thinking-dot size-1 rounded-full bg-primary [animation-delay:280ms]" />
                          </span>
                        </div>
                      </div>
                    )}
                  </div>
                  <span className="pt-1 text-xs text-muted-foreground">
                    {complete ? "Recorded" : isActive ? "Working" : "Queued"}
                  </span>
                </li>
              );
            })}
          </ol>
          </div>

          <aside
            key={activeStage}
            className="recovery-context-enter h-fit border-l pl-6 lg:pl-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.12em] text-muted-foreground">
              Run context
            </p>
            <p className="mt-3 text-sm font-medium">
              {active ? active.owner : "Workflow coordinator"}
            </p>
            <p className="mt-1 text-sm leading-6 text-muted-foreground">
              {active
                ? "Working with scoped operational data and allow-listed tools."
                : "All evidence has been recorded. Opening the review now."}
            </p>
            <Separator className="my-4" />
            <div className="flex gap-2.5 text-xs leading-5 text-muted-foreground">
              <ShieldCheck className="mt-0.5 size-4 shrink-0 text-primary" />
              <p>
                The workflow pauses after validation. A Centre Manager or Admin
                must review and approve the proposed dispatch change.
              </p>
            </div>
          </aside>
        </div>
      </section>
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
