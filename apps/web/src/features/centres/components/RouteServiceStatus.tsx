import { ArrowRight, BusFront, Clock3, MapPin } from "lucide-react";
import { StatusBadge } from "@/components/shared/StatusBadge";
import type { CentreRouteService, ServiceRun } from "@/mock/centre-services";

export function RouteServiceStatus({
  service,
}: {
  service?: CentreRouteService;
}) {
  if (!service) return null;
  const current = service.runs.find((run) => run.state === "In service");
  const boarding = service.runs.find((run) => run.state === "Boarding");
  const following = service.runs.filter((run) => run.state === "Scheduled");
  return (
    <section className="overflow-hidden rounded-lg border bg-card">
      <div className="flex flex-col justify-between gap-4 border-b p-5 md:flex-row md:items-center">
        <div>
          <div className="flex items-center gap-2">
            <span className="rounded-md bg-primary px-2 py-1 text-xs font-semibold text-primary-foreground">
              Route {service.route}
            </span>
            <span className="flex items-center gap-1 text-sm text-muted-foreground">
              <Clock3 className="size-3.5" /> Every {service.intervalMinutes}{" "}
              min
            </span>
          </div>
          <h2 className="mt-3 text-lg font-semibold">
            {service.origin} <ArrowRight className="mx-1 inline size-4" />{" "}
            {service.destination}
          </h2>
        </div>
        <div className="rounded-md border bg-muted/30 px-4 py-2 text-sm">
          <span className="text-muted-foreground">Operating bay</span>
          <strong className="ml-3">{service.bay}</strong>
        </div>
      </div>
      <div className="grid lg:grid-cols-[1fr_1fr_1.2fr]">
        <RunCard
          label="Bus in service"
          run={current}
          extra={
            <span className="flex items-center gap-1 text-xs text-muted-foreground">
              <MapPin className="size-3" /> {service.currentLocation}
            </span>
          }
        />
        <RunCard label="Boarding now" run={boarding} emphasized />
        <div className="p-4">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
            Following departures
          </p>
          <div className="mt-3 flex gap-2">
            {following.map((run) => (
              <div
                key={run.time}
                className="min-w-0 flex-1 rounded-md border bg-muted/20 p-3"
              >
                <p className="font-semibold">{run.time}</p>
                <p className="mt-1 truncate text-xs text-muted-foreground">
                  {run.vehicle}
                </p>
                <p className="mt-1 text-[11px] text-muted-foreground">
                  {run.detail}
                </p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

function RunCard({
  label,
  run,
  extra,
  emphasized = false,
}: {
  label: string;
  run?: ServiceRun;
  extra?: React.ReactNode;
  emphasized?: boolean;
}) {
  if (!run) return null;
  return (
    <div
      className={`border-b p-4 lg:border-b-0 lg:border-r ${emphasized ? "bg-primary/5" : ""}`}
    >
      <div className="flex items-center justify-between">
        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          {label}
        </p>
        <StatusBadge
          label={run.state}
          tone={run.state === "Boarding" ? "warning" : "good"}
        />
      </div>
      <div className="mt-3 flex items-center gap-3">
        <div className="flex size-9 items-center justify-center rounded-md bg-primary/10 text-primary">
          <BusFront className="size-4" />
        </div>
        <div>
          <p className="font-semibold">
            {run.time} · {run.vehicle}
          </p>
          <p className="text-xs text-muted-foreground">{run.detail}</p>
        </div>
      </div>
      {extra && <div className="mt-3">{extra}</div>}
    </div>
  );
}
