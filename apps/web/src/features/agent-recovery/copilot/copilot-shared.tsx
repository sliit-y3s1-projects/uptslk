import { cn } from "@/lib/utils";
import { errorMessage } from "./copilot-utils";

export function ErrorLine({ error }: { error: unknown }) {
  const message = errorMessage(error);
  return message ? (
    <p className="mt-2 text-sm text-destructive">{message}</p>
  ) : null;
}

export function SourceBadge({
  label,
  tone = "neutral",
}: {
  label: string;
  tone?: "neutral" | "good" | "warning" | "info";
}) {
  return (
    <span
      className={cn(
        "inline-flex rounded-md border px-1.5 py-0.5 text-[11px] font-medium",
        tone === "good" && "border-emerald-200 bg-emerald-50 text-emerald-700",
        tone === "warning" && "border-amber-200 bg-amber-50 text-amber-800",
        tone === "info" && "border-indigo-200 bg-indigo-50 text-indigo-700",
        tone === "neutral" && "border-slate-200 bg-slate-50 text-slate-600",
      )}
    >
      {label}
    </span>
  );
}

export function ScoreBar({
  label,
  points,
  max,
  detail,
}: {
  label: string;
  points: number;
  max: number;
  detail: string;
}) {
  const share = max === 0 ? 0 : Math.max(0, Math.min(1, points / max));
  return (
    <div title={detail}>
      <div className="flex justify-between text-[11px] text-muted-foreground">
        <span>{label}</span>
        <span className="tabular-nums">
          {points}/{max}
        </span>
      </div>
      <div className="mt-0.5 h-1.5 overflow-hidden rounded-full bg-slate-200">
        <div
          className={cn(
            "h-full rounded-full",
            share >= 0.7
              ? "bg-emerald-500"
              : share >= 0.4
                ? "bg-amber-500"
                : "bg-red-500",
          )}
          style={{ width: `${share * 100}%` }}
        />
      </div>
    </div>
  );
}
