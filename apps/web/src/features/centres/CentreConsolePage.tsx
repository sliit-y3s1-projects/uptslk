import { useState } from "react";
import { ChevronDown, Loader2, UsersRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Link } from "react-router";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useCentre } from "./hooks/useCentres";
import { useTrips } from "@/features/operations/hooks/useTrips";

type Departure = {
  id: string;
  time: string;
  bay: string;
  route: string;
  destination: string;
  vehicle: string;
  status: string;
  capacity: number;
  occupied: number;
  available: number;
  isFull: boolean;
};

function departureTone(status: string) {
  if (status === "Delayed") return "danger" as const;
  if (status === "Boarding") return "warning" as const;
  return "good" as const;
}

export function CentreConsolePage({ centreId }: { centreId?: string }) {
  const { user } = useAuth();
  const effectiveCentreId = centreId ?? user?.centreId;
  const { data: centre, isLoading, error } = useCentre(effectiveCentreId);
  const {
    data: trips = [],
    isLoading: tripsLoading,
    error: tripsError,
  } = useTrips({ terminalId: effectiveCentreId });
  const departures: Departure[] = trips
    .filter((trip) => !["Completed", "Cancelled"].includes(trip.status))
    .map((trip) => ({
      id: trip.id,
      time: new Date(trip.scheduledTime).toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit",
      }),
      bay: trip.bay,
      route: trip.routeNumber,
      destination: trip.destination ?? trip.routeName,
      vehicle: trip.vehicle,
      status: trip.status,
      capacity: trip.capacity,
      occupied: trip.occupied,
      available: trip.available,
      isFull: trip.isFull,
    }));
  const [selected, setSelected] = useState<Departure | undefined>(undefined);

  if (isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        <Loader2 className="animate-spin text-primary" />
      </main>
    );
  if (error || tripsError)
    return (
      <main className="flex flex-1 items-center justify-center p-4 text-red-500">
        Failed to load centre.
      </main>
    );
  if (!centre)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        Centre not found.
      </main>
    );

  const baySlots = centre.bays.map((bay) => bay.code);

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <section className="flex flex-col justify-between gap-3 rounded-xl border border-emerald-600 bg-emerald-700 p-6 text-white md:flex-row md:items-end">
        <div>
          <p className="text-sm text-emerald-100">Centre operations overview</p>
          <h1 className="mt-1 text-2xl font-semibold">{centre.name}</h1>
          <p className="mt-2 text-sm text-emerald-100">
            {centre.description || "Active operations"}
          </p>
        </div>
        <div className="grid grid-cols-3 gap-2 text-sm">
          <Metric
            label="Bays active"
            value={`${departures.length}/${baySlots.length}`}
            href="/operations/bays"
          />
          <Metric
            label="Next departure"
            value={departures[0]?.time ?? "--"}
            href="/operations/dispatch"
          />
          <Metric
            label="Boarding now"
            value={String(
              departures.filter((item) => item.status === "Boarding").length,
            )}
            href="/operations/dispatch"
          />
        </div>
      </section>
      {tripsLoading ? (
        <p className="rounded-lg border bg-card p-5 text-sm text-muted-foreground">
          Loading live departures...
        </p>
      ) : (
        <section className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_400px]">
          <TerminalMap
            bays={baySlots}
            departures={departures}
            selected={selected}
            onSelect={setSelected}
          />
          <CapacitySummary departure={selected} />
        </section>
      )}
      <section className="rounded-lg border bg-card">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <div>
            <h2 className="font-semibold">Upcoming departures</h2>
            <p className="text-sm text-muted-foreground">
              Live trips and bay assignments from the operations API.
            </p>
          </div>
          <Button variant="outline">
            All departures <ChevronDown />
          </Button>
        </div>
        <div className="divide-y">
          {!tripsLoading && departures.length === 0 && (
            <p className="p-6 text-sm text-muted-foreground">
              No upcoming trips are scheduled for this centre.
            </p>
          )}
          {departures.map((departure) => (
            <button
              type="button"
              key={departure.id}
              onClick={() => setSelected(departure)}
              className="grid w-full gap-2 px-4 py-3 text-left hover:bg-muted/40 md:grid-cols-[80px_100px_minmax(0,1fr)_130px_100px]"
            >
              <p className="font-semibold">{departure.time}</p>
              <p className="text-sm text-muted-foreground">{departure.bay}</p>
              <p>
                <span className="font-medium">
                  {departure.route} - {departure.destination}
                </span>
                <span className="ml-2 text-sm text-muted-foreground">
                  {departure.vehicle}
                </span>
              </p>
              <StatusBadge
                label={departure.status}
                tone={departureTone(departure.status)}
              />
              <p
                className={`text-sm font-medium ${departure.isFull ? "text-rose-600" : "text-emerald-700"}`}
              >
                {departure.isFull
                  ? "Full"
                  : `${departure.available} spaces left`}
              </p>
            </button>
          ))}
        </div>
      </section>
    </main>
  );
}

function TerminalMap({
  bays,
  departures,
  selected,
  onSelect,
}: {
  bays: string[];
  departures: Departure[];
  selected?: Departure;
  onSelect: (departure: Departure) => void;
}) {
  return (
    <section className="rounded-lg border bg-card">
      <div className="flex flex-col gap-2 border-b px-5 py-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="font-semibold">Bay status</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Current departures and boarding capacity at this terminal.
          </p>
        </div>
        <p className="text-sm text-muted-foreground">
          {departures.length} active ·{" "}
          {Math.max(0, bays.length - departures.length)} open
        </p>
      </div>
      <div className="grid gap-3 p-4 sm:grid-cols-2 xl:grid-cols-3">
        {bays.length === 0 && (
          <p className="px-5 py-10 text-center text-sm text-muted-foreground">
            No boarding bays have been configured for this centre.
          </p>
        )}
        {bays.map((bay) => {
          const departure = departures.find((item) => item.bay === bay);
          const active = departure?.id === selected?.id;
          if (!departure)
            return (
              <div
                key={bay}
                className="min-h-36 rounded-xl border border-emerald-200 bg-emerald-50/70 p-4"
              >
                <div className="flex items-center justify-between">
                  <span className="rounded-md border border-emerald-200 bg-white/70 px-2 py-1 text-sm font-bold text-emerald-900">
                    {bay}
                  </span>
                  <span className="text-xs font-semibold text-emerald-800">
                    Open
                  </span>
                </div>
                <p className="mt-10 font-medium text-emerald-950">Available</p>
                <p className="mt-1 text-sm text-emerald-800/80">
                  No departure assigned
                </p>
              </div>
            );
          const colour = departure.isFull
            ? "border-rose-200 bg-rose-50/70"
            : departure.status === "Boarding"
              ? "border-amber-300 bg-amber-50/80"
              : "border-primary/35 bg-primary/[0.05]";
          return (
            <button
              key={bay}
              type="button"
              onClick={() => onSelect(departure)}
              className={`min-h-36 rounded-xl border p-4 text-left transition hover:-translate-y-0.5 hover:shadow-sm ${colour} ${active ? "ring-2 ring-primary ring-offset-2" : ""}`}
            >
              <div className="flex items-start justify-between gap-3">
                <span className="rounded-md border border-current/15 bg-white/50 px-2 py-1 text-sm font-bold">
                  {bay}
                </span>
                <StatusBadge
                  label={departure.status}
                  tone={departureTone(departure.status)}
                />
              </div>
              <p className="mt-5 text-base font-semibold">
                {departure.route} · {departure.destination}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                {departure.vehicle}
              </p>
              <div className="mt-4 flex items-center justify-between border-t border-current/10 pt-3">
                <span className="text-sm font-semibold">{departure.time}</span>
                <span
                  className={`text-xs font-semibold ${departure.isFull ? "text-rose-700" : "text-emerald-700"}`}
                >
                  {departure.isFull
                    ? "Full"
                    : `${departure.available} spaces left`}
                </span>
              </div>
            </button>
          );
        })}
      </div>
    </section>
  );
}

function CapacitySummary({ departure }: { departure?: Departure }) {
  if (!departure)
    return (
      <aside className="rounded-lg border bg-card p-5">
        <h2 className="font-semibold">Departure capacity</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Choose an active bay to view its passenger count.
        </p>
      </aside>
    );
  const fill = departure.capacity
    ? Math.round((departure.occupied / departure.capacity) * 100)
    : 0;
  return (
    <aside className="rounded-lg border bg-card p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="font-semibold">Departure capacity</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {departure.vehicle} · {departure.route} to {departure.destination}
          </p>
        </div>
        <UsersRound className="size-5 text-primary" />
      </div>
      <div className="mt-6 rounded-lg border border-slate-300 bg-muted/30 p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div className="min-w-0">
            <p className="text-3xl font-semibold">
              {departure.occupied}
              <span className="text-lg text-muted-foreground">
                {" "}
                / {departure.capacity}
              </span>
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Passengers with active boarding passes
            </p>
          </div>
          <span
            className={`self-start whitespace-nowrap rounded-full px-4 py-2 text-sm font-semibold sm:self-end ${departure.isFull ? "bg-rose-100 text-rose-700" : "bg-emerald-100 text-emerald-800"}`}
          >
            {departure.isFull ? "Full" : `${departure.available} spaces left`}
          </span>
        </div>
        <div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-200">
          <div
            className={`h-full rounded-full ${departure.isFull ? "bg-rose-500" : "bg-emerald-500"}`}
            style={{ width: `${fill}%` }}
          />
        </div>
      </div>
      <p className="mt-4 text-sm text-muted-foreground">
        Urban services do not assign individual seats. This count is used to
        prevent overbooking.
      </p>
    </aside>
  );
}

function Metric({
  label,
  value,
  href,
}: {
  label: string;
  value: string;
  href: string;
}) {
  return (
    <Link
      to={href}
      className="rounded-md px-3 py-2 transition hover:bg-white/10"
    >
      <p className="text-xs text-white/75">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </Link>
  );
}
