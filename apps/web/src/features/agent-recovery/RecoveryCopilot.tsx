import { useState } from "react";
import { cn } from "@/lib/utils";
import { DispatchPanel } from "./copilot/DispatchPanel";
import { FleetPanel } from "./copilot/FleetPanel";
import { NetworkPanel } from "./copilot/NetworkPanel";
import { PassengerPanel } from "./copilot/PassengerPanel";
import type { WorkflowStatus } from "./agent-recovery.types";

export type CopilotTab = "fleet" | "network" | "passenger" | "dispatch";

const tabs: { id: CopilotTab; label: string; hint: string }[] = [
  { id: "fleet", label: "Fleet", hint: "Compare simulated options" },
  { id: "network", label: "Network", hint: "See what else is disturbed" },
  { id: "passenger", label: "Passengers", hint: "Review the messages" },
  { id: "dispatch", label: "Dispatch", hint: "Reject, revise, remember" },
];

// The coordinator's side of the recovery: each agent proposes, the person steers, the agent adapts.
export function RecoveryCopilot({
  workflowId,
  status,
  pendingDecision,
  canRevise,
  tab,
  onTabChange,
}: {
  workflowId: string;
  status: WorkflowStatus;
  pendingDecision: boolean;
  canRevise: boolean;
  tab: CopilotTab;
  onTabChange: (tab: CopilotTab) => void;
}) {
  const [opened, setOpened] = useState<Set<CopilotTab>>(new Set([tab]));
  const completed = status === "Completed";
  if (!pendingDecision && !completed) return null;

  const visible = completed ? tabs.filter((item) => item.id === "passenger") : tabs;
  const active = completed ? "passenger" : tab;
  const choose = (next: CopilotTab) => {
    setOpened((current) => new Set(current).add(next));
    onTabChange(next);
  };

  return (
    <section
      id="recovery-copilot"
      className="rounded-xl border border-slate-300 bg-card"
    >
      <div className="border-b px-5 py-4">
        <h2 className="font-semibold">Coordinator tools</h2>
        <p className="text-sm text-muted-foreground">
          {completed
            ? "What the passenger agent sent after approval."
            : "The agents proposed a recovery. Steer it here before a manager decides. Any change goes through the safety checks again."}
        </p>
      </div>
      <div className="flex flex-wrap gap-2 border-b px-5 py-3">
        {visible.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => choose(item.id)}
            className={cn(
              "rounded-lg border px-3 py-1.5 text-left text-sm transition-colors",
              active === item.id
                ? "border-primary bg-primary/5 font-medium"
                : "border-slate-200 hover:bg-muted",
            )}
          >
            {item.label}
            <span className="block text-[11px] font-normal text-muted-foreground">
              {item.hint}
            </span>
          </button>
        ))}
      </div>
      <div className="p-5">
        {active === "fleet" && opened.has("fleet") && <FleetPanel workflowId={workflowId} />}
        {active === "network" && opened.has("network") && <NetworkPanel workflowId={workflowId} />}
        {active === "passenger" && (
          <PassengerPanel workflowId={workflowId} editable={pendingDecision} />
        )}
        {active === "dispatch" && (
          <DispatchPanel workflowId={workflowId} canRevise={canRevise} pending={pendingDecision} />
        )}
      </div>
    </section>
  );
}
