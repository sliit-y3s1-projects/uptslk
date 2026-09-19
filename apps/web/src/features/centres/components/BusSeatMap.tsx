import {
  Accessibility,
  Armchair,
  DoorOpen,
  Gauge as SteeringWheel,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import type { Departure } from "@/mock/centres";

type SeatState = "occupied" | "reserved" | "available" | "accessible";

export function BusSeatMap({ departure }: { departure?: Departure }) {
  if (!departure)
    return (
      <aside className="rounded-lg border bg-card p-5">
        <h2 className="font-semibold">Bus seating</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Choose an active bay to inspect its bus.
        </p>
      </aside>
    );

  const capacity = 44;
  const occupied = Math.round((departure.occupancy / 100) * capacity);
  const reserved = Math.min(4, Math.max(1, Math.round(capacity * 0.08)));
  const rows = Array.from({ length: 10 }, (_, row) => [
    row * 4 + 1,
    row * 4 + 2,
    row * 4 + 3,
    row * 4 + 4,
  ]);
  const stateFor = (seat: number): SeatState => {
    if (seat === 1 || seat === 2) return "accessible";
    if (seat <= occupied) return "occupied";
    if (seat <= occupied + reserved) return "reserved";
    return "available";
  };

  return (
    <aside className="rounded-lg border bg-card p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="font-semibold">Bus seating</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {departure.vehicle} · {departure.route} to {departure.destination}
          </p>
        </div>
        <span className="rounded-md bg-muted px-2 py-1 text-xs font-medium">
          44 seats
        </span>
      </div>
      <div className="mt-4 flex flex-wrap gap-x-3 gap-y-2 text-[11px] text-muted-foreground">
        <Legend state="occupied" label="Occupied" />
        <Legend state="reserved" label="Reserved" />
        <Legend state="available" label="Available" />
        <Legend state="accessible" label="Accessible" />
      </div>
      <div className="mx-auto mt-4 w-full max-w-[370px]">
        <div className="border-4 border-slate-300 bg-slate-100 p-3 shadow-inner">
          <div className="bg-slate-700 p-2.5 text-slate-100">
            <div className="grid grid-cols-[48px_1fr_58px] items-center gap-2">
              <div className="flex size-10 items-center justify-center bg-slate-600">
                <SteeringWheel className="size-5" />
              </div>
              <span className="text-center text-[9px] font-medium uppercase tracking-[0.18em]">
                Front windscreen
              </span>
              <span className="text-right text-[8px] uppercase tracking-wider text-slate-300">
                Driver cabin
              </span>
            </div>
          </div>
          <div className="mt-2 grid grid-cols-[1fr_42px_1fr] items-stretch border border-slate-300 bg-white p-2.5">
            <div className="flex items-center justify-center text-[9px] font-medium uppercase tracking-wider text-slate-400">
              Left
            </div>
            <div className="flex items-center justify-center text-[8px] font-medium uppercase tracking-wider text-slate-400">
              Aisle
            </div>
            <div className="flex items-center justify-center gap-1 text-[9px] font-medium uppercase tracking-wider text-slate-400">
              <DoorOpen className="size-3.5" /> Entry
            </div>
          </div>
          <div className="mt-2 border border-slate-300 bg-white p-2.5">
            <div className="space-y-1.5">
              {rows.map((row, index) => (
                <div
                  key={index}
                  className="grid grid-cols-[1fr_1fr_34px_1fr_1fr] gap-1.5"
                >
                  <Seat number={row[0]} state={stateFor(row[0])} />
                  <Seat number={row[1]} state={stateFor(row[1])} />
                  <span className="flex items-center justify-center border-y border-dashed border-slate-200 text-[9px] text-slate-400">
                    {index + 1}
                  </span>
                  <Seat number={row[2]} state={stateFor(row[2])} />
                  <Seat number={row[3]} state={stateFor(row[3])} />
                </div>
              ))}
            </div>
            <div className="mt-2 grid grid-cols-[1fr_1fr_34px_1fr_1fr] gap-1.5 border-t border-dashed border-slate-300 pt-2">
              <Seat number={41} state={stateFor(41)} />
              <Seat number={42} state={stateFor(42)} />
              <span className="flex items-center justify-center text-[8px] text-slate-400">
                Rear
              </span>
              <Seat number={43} state={stateFor(43)} />
              <Seat number={44} state={stateFor(44)} />
            </div>
          </div>
          <div className="mt-2 flex items-center justify-between px-2 text-[9px] font-medium uppercase tracking-wider text-slate-400">
            <span>Rear seating</span>
            <span className="flex items-center gap-1">
              <DoorOpen className="size-3.5" /> Emergency exit
            </span>
          </div>
        </div>
      </div>
      <div className="mt-4 grid grid-cols-3 divide-x rounded-md border bg-muted/30 py-2 text-center">
        <Count label="Occupied" value={occupied} />
        <Count label="Reserved" value={reserved} />
        <Count label="Available" value={capacity - occupied - reserved} />
      </div>
      <Button className="mt-4 w-full" variant="outline">
        <Armchair /> Open passenger manifest
      </Button>
    </aside>
  );
}

function Seat({ number, state }: { number: number; state: SeatState }) {
  const styles = {
    occupied: "border-primary bg-primary text-primary-foreground",
    reserved: "border-amber-300 bg-amber-100 text-amber-800",
    available: "border-slate-300 bg-white text-slate-600",
    accessible: "border-blue-300 bg-blue-100 text-blue-700",
  };
  return (
    <div
      title={`Seat ${number}: ${state}`}
      className={`flex h-8 items-center justify-center rounded-md border text-[10px] font-semibold ${styles[state]}`}
    >
      {state === "accessible" ? <Accessibility className="size-3.5" /> : number}
    </div>
  );
}
function Legend({ state, label }: { state: SeatState; label: string }) {
  const colours = {
    occupied: "bg-primary",
    reserved: "bg-amber-300",
    available: "border bg-white",
    accessible: "bg-blue-300",
  };
  return (
    <span className="flex items-center gap-1.5">
      <i className={`size-2.5 rounded-sm ${colours[state]}`} />
      {label}
    </span>
  );
}
function Count({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <p className="text-sm font-semibold">{value}</p>
      <p className="text-[10px] text-muted-foreground">{label}</p>
    </div>
  );
}
