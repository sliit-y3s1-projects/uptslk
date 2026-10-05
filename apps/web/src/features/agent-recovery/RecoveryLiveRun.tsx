import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Bus,
  Check,
  FileSearch,
  Hand,
  ListChecks,
  LoaderCircle,
  Minus,
  Network,
  ShieldCheck,
  Sparkles,
  Ticket,
  UserCheck,
  Wrench,
  X,
  type LucideIcon,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { useAuth } from "@/hooks/useAuth";
import { agentRecoveryApi } from "./agent-recovery.api";
import type {
  RecoveryIncident,
  RecoveryWorkflowDetail,
} from "./agent-recovery.types";

type Plan = RecoveryWorkflowDetail["plan"];
type StepState = "queued" | "active" | "done" | "failed" | "skipped" | "waiting";

const TERMINAL = new Set(["Completed", "Failed", "Skipped", "Blocked", "Rejected"]);
const REVEAL_INTERVAL_MS = 650;
const HAND_OFF_DELAY_MS = 1100;

const ownerIcon = (owner: string): LucideIcon => {
  if (owner.startsWith("Network")) return Network;
  if (owner.startsWith("Fleet")) return Bus;
  if (owner.startsWith("Dispatch")) return UserCheck;
  if (owner.startsWith("Passenger")) return Ticket;
  if (owner.startsWith("Safety")) return ShieldCheck;
  return Hand;
};

const formatMs = (ms: number) =>
  ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} s`;

const formatElapsed = (seconds: number) =>
  `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;

function useElapsedSeconds(running: boolean) {
  const [seconds, setSeconds] = useState(0);
  useEffect(() => {
    if (!running) return;
    const timer = window.setInterval(() => setSeconds((value) => value + 1), 1000);
    return () => window.clearInterval(timer);
  }, [running]);
  return seconds;
}

/**
 * Follows the persisted workflow while the start request is still in flight.
 * The API saves the plan and every specialist result as it goes, so polling the
 * normal detail endpoint shows real progress with no extra backend surface.
 */
function useLiveWorkflow(incidentId: string, workflowId: string | null) {
  const { user } = useAuth();
  const running = useQuery({
    queryKey: ["agent-recovery-live-list", incidentId],
    queryFn: () => agentRecoveryApi.list(user?.centreId),
    enabled: !workflowId,
    refetchInterval: 700,
    retry: false,
  });
  const activeId =
    running.data?.find(
      (item) => item.incidentId === incidentId && item.status === "Running",
    )?.id ?? null;
  const id = workflowId ?? activeId;
  const detail = useQuery({
    queryKey: ["agent-recovery-live", id],
    queryFn: () => agentRecoveryApi.detail(id!),
    enabled: !!id,
    refetchInterval: (query) =>
      query.state.data?.summary.status === "Running" ? 600 : false,
    retry: false,
  });
  return detail.data ?? null;
}

export function RecoveryLiveRun({
  incident,
  workflowId,
  onDone,
}: {
  incident?: RecoveryIncident;
  workflowId: string | null;
  onDone: () => void;
}) {
  const detail = useLiveWorkflow(incident?.id ?? "", workflowId);
  const plan: Plan = useMemo(
    () => [...(detail?.plan ?? [])].sort((a, b) => a.order - b.order),
    [detail?.plan],
  );
  const serverDone =
    !!workflowId && !!detail && detail.summary.status !== "Running";
  const leadingSettled = useMemo(() => {
    let count = 0;
    for (const step of plan) {
      if (!TERMINAL.has(step.status)) break;
      count++;
    }
    return count;
  }, [plan]);
  const target = serverDone ? plan.length : leadingSettled;

  // Reveal settled steps one at a time so each result is readable. The data is
  // real; only the pace at which it is shown is smoothed.
  const [shown, setShown] = useState(0);
  useEffect(() => {
    if (shown >= target) return;
    const timer = window.setTimeout(
      () => setShown((value) => value + 1),
      REVEAL_INTERVAL_MS,
    );
    return () => window.clearTimeout(timer);
  }, [shown, target]);

  const complete = serverDone && shown >= plan.length;
  useEffect(() => {
    if (!complete) return;
    const timer = window.setTimeout(onDone, HAND_OFF_DELAY_MS);
    return () => window.clearTimeout(timer);
  }, [complete, onDone]);

  const elapsed = useElapsedSeconds(!complete);
  const planning = plan.length === 0 && !serverDone;
  const failed = serverDone && detail?.summary.status === "Failed";
  const awaitingApproval = serverDone && detail?.summary.status === "PausedForApproval";
  const activeStep = !planning && !complete ? plan[shown] : undefined;
  const agentCount = plan.filter(
    (step) => step.owner !== "Safety Validation" && step.owner !== "Manager Approval",
  ).length;

  const headline = complete
    ? failed
      ? "Run stopped safely"
      : "Ready for your review"
    : planning
      ? "Planning the recovery"
      : activeStep?.owner === "Safety Validation"
        ? "Running safety checks"
        : "Specialists are assessing";
  const subline = complete
    ? failed
      ? "No trip changes were made. Opening the audit trail."
      : "Nothing has been applied yet. Opening the proposal for your decision."
    : planning
      ? "The planner is choosing which specialists this incident needs."
      : activeStep
        ? `${activeStep.owner} · ${activeStep.purpose}`
        : "Finalising the audit record.";

  return (
    <main className="flex flex-1 justify-center bg-background px-5 py-10 sm:px-8 lg:py-14">
      <section className="recovery-workspace-enter w-full max-w-5xl">
        <header className="flex items-start gap-4">
          <span
            aria-hidden="true"
            className={`mt-1 flex size-10 shrink-0 items-center justify-center rounded-full border ${
              complete
                ? failed
                  ? "border-destructive/40 bg-destructive/10 text-destructive"
                  : "border-emerald-500/40 bg-emerald-500/10 text-emerald-700"
                : "recovery-orb border-primary/30 bg-primary/10 text-primary"
            }`}
          >
            {complete ? (
              failed ? (
                <X className="size-5" />
              ) : (
                <Check className="size-5" />
              )
            ) : (
              <Sparkles className="size-5" />
            )}
          </span>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-semibold tracking-tight">
                {headline}
              </h1>
              {!complete && (
                <span className="text-xs tabular-nums text-muted-foreground">
                  {formatElapsed(elapsed)}
                </span>
              )}
            </div>
            <p
              aria-live="polite"
              className={`mt-1 text-sm leading-6 text-muted-foreground ${
                complete ? "" : "recovery-shimmer-text"
              }`}
            >
              {subline}
            </p>
          </div>
        </header>

        <div className="mt-9 grid gap-10 lg:grid-cols-[minmax(0,1fr)_260px] lg:gap-14">
          <ol aria-label="Recovery run activity">
            <TimelineItem
              icon={FileSearch}
              state="done"
              title="Read the incident"
              meta="Recorded"
              last={false}
            >
              {incident && (
                <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                  <span className="font-medium text-foreground">
                    {incident.title}
                  </span>
                  <Badge variant="outline">{incident.type}</Badge>
                  <Badge variant="outline">{incident.severity}</Badge>
                  {incident.tripRouteNumber && (
                    <span>{incident.tripRouteNumber}</span>
                  )}
                </div>
              )}
            </TimelineItem>

            <TimelineItem
              icon={ListChecks}
              state={planning ? "active" : "done"}
              title="Plan the recovery"
              meta={
                planning
                  ? "Planning"
                  : detail
                    ? formatMs(detail.planning.planningDurationMs)
                    : undefined
              }
              last={false}
            >
              {planning ? (
                <ActiveHint>
                  Asking the planner to choose specialists and their order.
                </ActiveHint>
              ) : detail ? (
                <PlanSummary detail={detail} agentCount={agentCount} />
              ) : null}
            </TimelineItem>

            {plan.map((step, index) => {
              const settled = index < shown;
              const isActive = !complete && index === shown;
              const state = stepState(step, settled, isActive, awaitingApproval);
              return (
                <TimelineItem
                  key={`${step.order}-${step.owner}`}
                  icon={ownerIcon(step.owner)}
                  state={state}
                  title={step.title}
                  subtitle={step.owner}
                  meta={stateLabel(state, step.status)}
                  last={index === plan.length - 1}
                >
                  {isActive && <ActiveHint>{step.purpose}</ActiveHint>}
                  {settled && detail && (
                    <StepResult step={step} detail={detail} />
                  )}
                </TimelineItem>
              );
            })}
          </ol>

          <aside className="h-fit border-l pl-6 lg:pl-8">
            <p className="text-xs font-semibold uppercase tracking-[0.12em] text-muted-foreground">
              Run details
            </p>
            <dl className="mt-3 grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
              <dt className="text-muted-foreground">Workflow</dt>
              <dd className="truncate font-mono text-xs leading-5">
                {detail ? detail.summary.id.slice(0, 8) : "Creating…"}
              </dd>
              <dt className="text-muted-foreground">Planner</dt>
              <dd>
                {planning || !detail
                  ? "Working…"
                  : detail.planning.planningMode.startsWith("Fallback")
                    ? "Safe fallback"
                    : (detail.planning.modelName ?? detail.planning.planningMode)}
              </dd>
              <dt className="text-muted-foreground">Tokens</dt>
              <dd className="tabular-nums">
                {planning || !detail
                  ? "—"
                  : detail.planning.totalTokenCount.toLocaleString()}
              </dd>
              <dt className="text-muted-foreground">Revisions</dt>
              <dd className="tabular-nums">
                {detail ? detail.planning.replanCount : 0}
              </dd>
            </dl>
            <div className="mt-5 flex gap-2.5 border-t pt-4 text-xs leading-5 text-muted-foreground">
              <ShieldCheck className="mt-0.5 size-4 shrink-0 text-primary" />
              <p>
                Specialists use allow-listed tools only. Every result is
                validated and saved. Trips change only after a Centre Manager
                or Admin approves.
              </p>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}

function stepState(
  step: Plan[number],
  settled: boolean,
  active: boolean,
  awaitingApproval: boolean,
): StepState {
  if (active) return "active";
  if (!settled) return "queued";
  if (step.owner === "Manager Approval")
    return awaitingApproval ? "waiting" : "skipped";
  if (step.status === "Completed") return "done";
  if (step.status === "Failed") return "failed";
  return "skipped";
}

function stateLabel(state: StepState, status: string) {
  switch (state) {
    case "active":
      return "Working";
    case "done":
      return "Recorded";
    case "failed":
      return "Failed";
    case "waiting":
      return "Your decision";
    case "skipped":
      return status === "Rejected" ? "Rejected" : "Not run";
    default:
      return "Queued";
  }
}

function TimelineItem({
  icon: Icon,
  state,
  title,
  subtitle,
  meta,
  last,
  children,
}: {
  icon: LucideIcon;
  state: StepState;
  title: string;
  subtitle?: string;
  meta?: string;
  last: boolean;
  children?: ReactNode;
}) {
  const settledTone =
    state === "done"
      ? "border-emerald-600 bg-emerald-600 text-white"
      : state === "failed"
        ? "border-destructive bg-destructive text-white"
        : state === "waiting"
          ? "border-amber-500 bg-amber-100 text-amber-800"
          : state === "active"
            ? "border-primary bg-primary/5 text-primary ring-4 ring-primary/10"
            : "border-border bg-background text-muted-foreground";
  return (
    <li
      aria-current={state === "active" ? "step" : undefined}
      className={`relative grid grid-cols-[32px_minmax(0,1fr)_auto] gap-4 pb-7 last:pb-0 ${
        state === "active"
          ? "recovery-step-active"
          : state === "done"
            ? "recovery-step-complete"
            : ""
      }`}
    >
      {!last && (
        <span
          aria-hidden="true"
          className={`recovery-step-rail absolute left-[15px] top-8 h-[calc(100%-1.25rem)] w-px ${
            state === "done" ? "bg-emerald-500/50" : "bg-border"
          }`}
        />
      )}
      <span
        className={`recovery-step-icon relative z-10 flex size-8 items-center justify-center rounded-full border ${settledTone}`}
      >
        {state === "active" ? (
          <LoaderCircle className="size-3.5 animate-spin" />
        ) : state === "done" ? (
          <Check className="size-3.5" />
        ) : state === "failed" ? (
          <X className="size-3.5" />
        ) : state === "skipped" ? (
          <Minus className="size-3.5" />
        ) : (
          <Icon className="size-3.5" />
        )}
      </span>
      <div className="min-w-0 pt-1">
        <p
          className={`text-sm ${
            state === "queued" ? "text-muted-foreground" : "font-medium"
          }`}
        >
          {title}
          {subtitle && (
            <span className="ml-2 text-xs font-normal text-muted-foreground">
              {subtitle}
            </span>
          )}
        </p>
        {children && <div className="recovery-step-detail mt-2 max-w-xl">{children}</div>}
      </div>
      <span className="pt-1 text-xs text-muted-foreground">{meta}</span>
    </li>
  );
}

function ActiveHint({ children }: { children: ReactNode }) {
  return (
    <div>
      <p className="text-sm leading-6 text-muted-foreground">{children}</p>
      <span aria-hidden="true" className="mt-1.5 flex gap-0.5">
        <span className="recovery-thinking-dot size-1 rounded-full bg-primary" />
        <span className="recovery-thinking-dot size-1 rounded-full bg-primary [animation-delay:140ms]" />
        <span className="recovery-thinking-dot size-1 rounded-full bg-primary [animation-delay:280ms]" />
      </span>
    </div>
  );
}

function PlanSummary({
  detail,
  agentCount,
}: {
  detail: RecoveryWorkflowDetail;
  agentCount: number;
}) {
  const fallback = detail.planning.planningMode.startsWith("Fallback");
  return (
    <div className="space-y-1.5 text-sm text-muted-foreground">
      <p>
        {fallback
          ? `Used the safe fallback plan with ${agentCount} specialists.`
          : `${detail.planning.modelName ?? "The planner"} selected ${agentCount} specialists.`}
      </p>
      {fallback && detail.planning.planningFallbackReason && (
        <p className="text-amber-700">{detail.planning.planningFallbackReason}</p>
      )}
      {detail.planning.replanCount > 0 && (
        <p>Plan revised after validation feedback.</p>
      )}
    </div>
  );
}

function StepResult({
  step,
  detail,
}: {
  step: Plan[number];
  detail: RecoveryWorkflowDetail;
}) {
  if (step.owner === "Safety Validation") {
    const checks = detail.validationResults;
    if (!checks.length) return null;
    return (
      <ul className="space-y-1 text-sm">
        {checks.map((check) => (
          <li
            key={`${check.phase}-${check.check}`}
            className="flex items-start gap-2"
          >
            {check.passed ? (
              <Check className="mt-0.5 size-3.5 shrink-0 text-emerald-600" />
            ) : (
              <X className="mt-0.5 size-3.5 shrink-0 text-destructive" />
            )}
            <span
              className={check.passed ? "text-muted-foreground" : "text-destructive"}
            >
              {check.check}
            </span>
          </li>
        ))}
      </ul>
    );
  }
  if (step.owner === "Manager Approval") return null;

  const record = detail.steps.filter((item) => item.agentName === step.owner).at(-1);
  if (!record) return null;
  const warnings = record.output?.warnings ?? [];
  return (
    <div className="space-y-2">
      {record.output?.summary && (
        <p className="text-sm leading-6">{record.output.summary}</p>
      )}
      {record.error && <p className="text-sm text-destructive">{record.error}</p>}
      {warnings.map((warning) => (
        <p key={warning} className="text-sm text-amber-700">
          {warning}
        </p>
      ))}
      <div className="flex flex-wrap items-center gap-1.5 text-xs text-muted-foreground">
        {record.toolCalls.map((call, index) => (
          <span
            key={`${call.tool}-${index}`}
            className="inline-flex items-center gap-1 rounded-md border bg-muted/40 px-2 py-0.5 font-mono"
          >
            <Wrench className="size-3" />
            {call.tool}
          </span>
        ))}
        <span className="tabular-nums">{formatMs(record.durationMs)}</span>
        {record.retryCount > 0 && <span>· retried {record.retryCount}×</span>}
      </div>
    </div>
  );
}
