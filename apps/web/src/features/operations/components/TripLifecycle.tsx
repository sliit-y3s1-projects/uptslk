import { Check, Clock3 } from "lucide-react";
import type { TripStatus } from "../types/trips";

const stages: TripStatus[] = ["Scheduled", "Ready", "Boarding", "Dispatched", "Completed"];

export function TripLifecycle({ status }: { status: TripStatus }) {
  const currentStatus = status === "Delayed" ? "Ready" : status === "Cancelled" ? "Scheduled" : status;
  const currentIndex = stages.indexOf(currentStatus);
  const isAttention = status === "Delayed" || status === "Cancelled";

  return <section className="mt-6" aria-label="Trip lifecycle progress">
    <div className="mb-5 flex items-center justify-between text-xs">
      <span className="text-muted-foreground">{status === "Completed" ? "Service completed" : `${Math.max(currentIndex + 1, 1)} of ${stages.length} operational stages`}</span>
      {status === "Delayed" && <span className="font-medium text-amber-700">Attention required before boarding</span>}
      {status === "Cancelled" && <span className="font-medium text-red-700">Service cancelled</span>}
    </div>
    <div className="relative grid grid-cols-5">
      {stages.slice(1).map((_, segmentOffset) => {
        const stageIndex = segmentOffset + 1;
        const isComplete = stageIndex <= currentIndex && status !== "Cancelled";
        return <span key={`connector-${stageIndex}`} className={`absolute top-4 z-0 h-0.5 ${isComplete ? "bg-primary" : "bg-border"}`} style={{ left: `calc(${10 + segmentOffset * 20}% + 1.25rem)`, width: "calc(20% - 2.5rem)" }} />;
      })}
      {stages.map((stage, index) => {
        const completed = index < currentIndex || status === "Completed";
        const current = index === currentIndex && status !== "Completed";
        const currentClasses = isAttention
          ? "border-amber-500 bg-amber-50 text-amber-700"
          : "border-primary bg-primary/10 text-primary";

        return <div key={stage} className={`relative isolate flex min-w-0 flex-col items-center text-center ${current ? "z-10" : "z-0"}`}>
          <span className={`relative z-10 flex size-8 items-center justify-center rounded-full border-2 bg-card ${completed ? "border-primary bg-primary text-primary-foreground" : current ? currentClasses : "border-border text-muted-foreground"}`}>
            {completed ? <Check className="size-4" strokeWidth={3} /> : current ? <span className={`relative size-2.5 rounded-full ${isAttention ? "bg-amber-500" : "bg-primary"} animate-pulse`} /> : <Clock3 className="size-3.5" />}
          </span>
          <p className={`mt-2 text-xs font-medium ${completed || current ? "text-foreground" : "text-muted-foreground"}`}>{stage}</p>
          <p className="mt-0.5 hidden text-[11px] text-muted-foreground sm:block">{completed ? "Complete" : current ? status : "Pending"}</p>
        </div>;
      })}
    </div>
  </section>;
}
