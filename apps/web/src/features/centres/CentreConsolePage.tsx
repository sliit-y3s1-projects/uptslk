import { useState } from "react";
import { BusFront, ChevronDown, MapPinned, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Link } from "react-router";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { RouteServiceStatus } from "@/features/centres/components/RouteServiceStatus";
import { BusSeatMap } from "@/features/centres/components/BusSeatMap";
import type { Departure } from "@/mock/centres";
import { useCentre } from "./hooks/useCentres";

function departureTone(status: string) {
  if (status === "Delayed") return "danger" as const;
  if (status === "Boarding") return "warning" as const;
  return "good" as const;
}

export function CentreConsolePage({ centreId }: { centreId?: string }) {
  const { user } = useAuth();
  const effectiveCentreId = centreId ?? user?.centreId;
  const { data: centre, isLoading, error } = useCentre(effectiveCentreId);

  // Removed mock data reads to satisfy API-only requirements
  const departures: Departure[] = [];
  const [selected, setSelected] = useState<Departure | undefined>(undefined);

  if (isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        <Loader2 className="animate-spin text-primary" />
      </main>
    );
  if (error)
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
      <section className="flex flex-col justify-between gap-3 rounded-xl bg-slate-900 p-6 text-white md:flex-row md:items-end">
        <div>
          <p className="text-sm text-slate-300">Centre operations overview</p>
          <h1 className="mt-1 text-2xl font-semibold">{centre.name}</h1>
          <p className="mt-2 text-sm text-slate-300">
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
      <RouteServiceStatus />
      <section className="grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_400px]">
        <TerminalMap
          bays={baySlots}
          departures={departures}
          selected={selected}
          onSelect={setSelected}
        />
        <BusSeatMap departure={selected} />
      </section>
      <section className="rounded-lg border bg-card">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <div>
            <h2 className="font-semibold">Upcoming departures</h2>
            <p className="text-sm text-muted-foreground">
              Mock operational schedule - not a live public timetable.
            </p>
          </div>
          <Button variant="outline">
            All departures <ChevronDown />
          </Button>
        </div>
        <div className="divide-y">
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
              <p className="text-sm font-medium">{departure.occupancy}% full</p>
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
    <section className="rounded-lg border bg-card p-5">
      <div className="flex items-start justify-between">
        <div>
          <h2 className="font-semibold">Bay & parking view</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Select a bay to inspect its departure and seating.
          </p>
        </div>
        <MapPinned className="size-5 text-primary" />
      </div>
      <div className="mt-5 rounded-lg border bg-slate-100 p-4">
        <div className="mb-4 flex items-center justify-center rounded border border-dashed bg-white py-3 text-xs font-medium text-slate-500">
          Passenger concourse - ticketing - waiting area
        </div>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {bays.map((bay) => {
            const departure = departures.find((item) => item.bay === bay);
            const active = departure?.id === selected?.id;
            return (
              <button
                key={bay}
                type="button"
                disabled={!departure}
                onClick={() => departure && onSelect(departure)}
                className={`min-h-24 rounded-md border p-3 text-left transition ${active ? "border-primary bg-primary/10 ring-2 ring-primary/20" : departure ? "bg-white hover:border-primary/50" : "border-dashed bg-slate-50 text-slate-400"}`}
              >
                <p className="text-xs font-semibold">{bay}</p>
                {departure ? (
                  <>
                    <BusFront className="mt-2 size-5 text-primary" />
                    <p className="mt-2 text-sm font-medium">
                      {departure.destination}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {departure.time} - {departure.occupancy}%
                    </p>
                  </>
                ) : (
                  <p className="mt-6 text-xs">Available</p>
                )}
              </button>
            );
          })}
        </div>
        <div className="mt-4 flex items-center justify-between rounded bg-white px-3 py-2 text-xs text-muted-foreground">
          <span>Bus access lane</span>
          <span>Entry &gt;</span>
        </div>
      </div>
    </section>
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
      <p className="text-xs text-slate-300">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </Link>
  );
}
