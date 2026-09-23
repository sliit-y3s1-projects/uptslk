import { BusFront, UsersRound } from "lucide-react";
import type { BayOperation } from "@/mock/centre-operations";

const stateStyles: Record<BayOperation["state"], string> = {
  Boarding: "border-amber-300 bg-amber-50/80",
  Occupied: "border-primary/35 bg-primary/[0.05]",
  Available: "border-emerald-200 bg-emerald-50/70",
  Closed: "border-rose-200 bg-rose-50/70 text-rose-800",
};

export function BayOperationsGrid({
  bays,
  selected,
  onSelect,
}: {
  bays: BayOperation[];
  selected?: BayOperation;
  onSelect: (bay: BayOperation) => void;
}) {
  return (
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
      {bays.map((bay) => (
        <button
          key={bay.bay}
          type="button"
          onClick={() => onSelect(bay)}
          className={`min-h-40 rounded-xl border p-4 text-left transition hover:-translate-y-0.5 hover:shadow-sm ${stateStyles[bay.state]} ${selected?.bay === bay.bay ? "ring-2 ring-primary ring-offset-2" : ""}`}
        >
          <div className="flex items-center justify-between">
            <span className="rounded-md border border-current/15 bg-white/50 px-2 py-1 text-sm font-bold">
              {bay.bay}
            </span>
            <span className="text-xs font-semibold">{bay.state}</span>
          </div>
          {bay.vehicle ? (
            <>
              <div className="mt-5 flex items-center gap-2">
                <BusFront className="size-4 text-primary" />
                <span className="text-sm font-medium">{bay.vehicle}</span>
              </div>
              <p className="mt-2 truncate text-base font-semibold">
                {bay.route} · {bay.destination}
              </p>
              <div className="mt-4 flex items-center justify-between border-t border-current/10 pt-3 text-xs text-muted-foreground">
                <span>Departs {bay.departure}</span>
                {(bay.queue ?? 0) > 0 && (
                  <span className="flex items-center gap-1">
                    <UsersRound className="size-3" /> {bay.queue} waiting
                  </span>
                )}
              </div>
            </>
          ) : (
            <p className="mt-12 text-sm font-medium">
              {bay.state === "Closed"
                ? "Unavailable for assignment"
                : "Ready for assignment"}
            </p>
          )}
        </button>
      ))}
    </div>
  );
}
