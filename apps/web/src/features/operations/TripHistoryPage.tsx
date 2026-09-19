import { useState } from "react";
import { ArrowLeft, Search } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useTripHistory } from "./hooks/useTrips";

export function TripHistoryPage() {
  const { user } = useAuth();
  const { data: trips = [], isLoading, error } = useTripHistory(user?.centreId);
  const [query, setQuery] = useState("");
  const history = trips.filter((trip) =>
    `${trip.id} ${trip.routeNumber} ${trip.routeName} ${trip.vehicle}`
      .toLowerCase()
      .includes(query.toLowerCase()),
  );
  if (isLoading) return <main className="p-5">Loading trip history...</main>;
  if (error)
    return (
      <main className="p-5 text-red-600">Failed to load trip history.</main>
    );
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Trip history"
        description="Review completed and cancelled departures retained for operational audit."
        action={
          <Button variant="outline" render={<Link to="/operations/dispatch" />}>
            <ArrowLeft /> Dispatch board
          </Button>
        }
      />
      <div className="relative max-w-lg">
        <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Search trip, route, or vehicle"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>
      <section className="overflow-hidden rounded-lg border bg-card">
        <div className="hidden grid-cols-[130px_150px_minmax(220px,1fr)_160px_120px] gap-3 border-b px-4 py-3 text-xs font-medium uppercase text-muted-foreground md:grid">
          <span>Trip</span>
          <span>Date</span>
          <span>Service</span>
          <span>Vehicle</span>
          <span>Status</span>
        </div>
        {history.map((trip) => (
          <Link
            key={trip.id}
            to={`/operations/dispatch/${trip.id}`}
            className="grid gap-3 border-b px-4 py-4 hover:bg-muted/40 md:grid-cols-[130px_150px_minmax(220px,1fr)_160px_120px] md:items-center"
          >
            <p className="font-medium">{trip.id}</p>
            <p className="text-sm text-muted-foreground">
              {new Date(trip.scheduledTime).toLocaleString()}
            </p>
            <p className="text-sm">
              Route {trip.routeNumber} · {trip.routeName}
            </p>
            <p className="text-sm text-muted-foreground">{trip.vehicle}</p>
            <StatusBadge
              label={trip.status}
              tone={trip.status === "Cancelled" ? "danger" : "good"}
            />
          </Link>
        ))}
        {history.length === 0 && (
          <p className="p-8 text-center text-sm text-muted-foreground">
            No historical trips match this search.
          </p>
        )}
      </section>
    </main>
  );
}
