import { BusFront, UsersRound } from "lucide-react";
import type { BayOperation } from "@/mock/centre-operations";

const stateStyles: Record<BayOperation["state"], string> = {
  Boarding: "border-amber-300 bg-amber-50",
  Occupied: "border-emerald-300 bg-emerald-50",
  Available: "border-dashed bg-background text-muted-foreground",
  Closed: "border-red-200 bg-red-50 text-red-700",
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
    <div className="rounded-xl border bg-slate-100 p-4">
      <div className="mb-4 rounded-lg border border-dashed bg-white px-4 py-3 text-center text-xs font-medium text-slate-500">
        Passenger concourse · gates · boarding control
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {bays.map((bay) => (
          <button
            key={bay.bay}
            type="button"
            onClick={() => onSelect(bay)}
            className={`min-h-32 rounded-lg border p-4 text-left transition hover:shadow-sm ${stateStyles[bay.state]} ${selected?.bay === bay.bay ? "ring-2 ring-primary ring-offset-2" : ""}`}
          >
            <div className="flex items-center justify-between">
              <strong>{bay.bay}</strong>
              <span className="text-xs font-medium">{bay.state}</span>
            </div>
            {bay.vehicle ? (
              <>
                <div className="mt-4 flex items-center gap-2">
                  <BusFront className="size-4 text-primary" />
                  <span className="text-sm font-medium">{bay.vehicle}</span>
                </div>
                <p className="mt-1 text-sm">
                  Route {bay.route} · {bay.destination}
                </p>
                <div className="mt-3 flex justify-between text-xs text-muted-foreground">
                  <span>{bay.departure}</span>
                  <span className="flex items-center gap-1">
                    <UsersRound className="size-3" /> {bay.queue} waiting
                  </span>
                </div>
              </>
            ) : (
              <p className="mt-8 text-sm">
                {bay.state === "Closed"
                  ? "Unavailable for assignment"
                  : "Ready for assignment"}
              </p>
            )}
          </button>
        ))}
      </div>
      <div className="mt-4 flex justify-between rounded-md bg-white px-4 py-2 text-xs text-muted-foreground">
        <span>Bus circulation lane</span>
        <span>Entrance → Exit</span>
      </div>
    </div>
  );
}
