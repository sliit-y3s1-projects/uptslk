import { BusFront, Clock3, ExternalLink, MapPin } from "lucide-react";
import { Link } from "react-router";
import { StatusBadge } from "@/components/shared/StatusBadge";
import type { TripListItem, TripStatus } from "../types/trips";

const columns: { label: string; statuses: TripStatus[] }[] = [
  { label: "Attention", statuses: ["Delayed"] },
  { label: "Boarding", statuses: ["Boarding"] },
  { label: "Ready & scheduled", statuses: ["Ready", "Scheduled"] },
  { label: "Dispatched", statuses: ["Dispatched"] },
];

export function DispatchBoard({
  items,
  selectedId,
  onSelect,
}: {
  items: TripListItem[];
  selectedId?: string;
  onSelect: (item: TripListItem) => void;
}) {
  return (
    <div className="grid gap-3 xl:grid-cols-4">
      {columns.map((column) => {
        const columnTrips = items.filter((item) =>
          column.statuses.includes(item.status),
        );
        return (
          <section key={column.label} className="rounded-lg border bg-card">
            <header className="flex items-center justify-between border-b px-4 py-3">
              <h2 className="text-sm font-semibold">{column.label}</h2>
              <span className="rounded-full bg-muted px-2 py-0.5 text-xs">
                {columnTrips.length}
              </span>
            </header>
            <div className="min-h-40 space-y-2 p-3">
              {columnTrips.map((item) => (
                <article
                  key={item.id}
                  className={`rounded-md border p-3 transition hover:border-primary/40 ${selectedId === item.id ? "border-primary bg-primary/5" : "bg-background"}`}
                >
                  <button
                    type="button"
                    onClick={() => onSelect(item)}
                    className="w-full text-left"
                  >
                    <div className="flex items-center justify-between">
                      <strong className="text-sm">
                        {new Date(item.scheduledTime).toLocaleTimeString([], {
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </strong>
                      <StatusBadge
                        label={item.status}
                        tone={
                          item.status === "Delayed"
                            ? "danger"
                            : item.status === "Boarding"
                              ? "warning"
                              : "good"
                        }
                      />
                    </div>
                    <p className="mt-3 font-medium">
                      Route {item.routeNumber} · {item.routeName}
                    </p>
                    <div className="mt-2 space-y-1 text-xs text-muted-foreground">
                      <p className="flex items-center gap-1.5">
                        <BusFront className="size-3" /> {item.vehicle}
                      </p>
                      <p className="flex items-center gap-1.5">
                        <MapPin className="size-3" /> {item.bay}
                      </p>
                      <p className="flex items-center gap-1.5">
                        <Clock3 className="size-3" /> {item.driver}
                      </p>
                    </div>
                  </button>
                  <Link
                    to={`/operations/dispatch/${item.id}`}
                    className="mt-3 flex items-center justify-end gap-1 border-t pt-2 text-xs font-medium text-primary"
                  >
                    Trip details <ExternalLink className="size-3" />
                  </Link>
                </article>
              ))}
            </div>
          </section>
        );
      })}
    </div>
  );
}
