import { cn } from "@/lib/utils";
import type { StatusTone } from "@/mock/mock-data";

export function StatusBadge({ label, tone }: { label: string; tone: StatusTone }) {
  return <span className={cn("inline-flex w-fit shrink-0 justify-self-start rounded-md border px-2 py-0.5 text-xs font-medium", tone === "good" && "border-emerald-200 bg-emerald-50 text-emerald-700", tone === "warning" && "border-amber-200 bg-amber-50 text-amber-700", tone === "danger" && "border-red-200 bg-red-50 text-red-700", (tone === "info" || label === "Centre") && "border-cyan-200 bg-cyan-50 text-cyan-700", tone === "neutral" && label !== "Centre" && "text-muted-foreground")}>{label}</span>;
}
