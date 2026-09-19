import { useMemo, useState } from "react";
import {
  CheckCircle2,
  History,
  Plus,
  Search,
  TriangleAlert,
} from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeading } from "@/components/shared/PageHeading";
import { DispatchBoard } from "@/features/operations/components/DispatchBoard";
import { useAuth } from "@/hooks/useAuth";
import { useTrips, useUpdateTripStatus } from "./hooks/useTrips";
import type { TripStatus } from "./types/trips";

export function DispatchPage() {
  const { user } = useAuth();
  const centreId = user?.centreId;
  const { data: trips = [], isLoading, error } = useTrips({ centreId });
  const statusMutation = useUpdateTripStatus();
  const scoped = useMemo(
    () =>
      trips.filter((trip) => !["Completed", "Cancelled"].includes(trip.status)),
    [trips],
  );
  const [selectedId, setSelectedId] = useState(scoped[0]?.id);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [serviceDate, setServiceDate] = useState("All");
  const selected = scoped.find((trip) => trip.id === selectedId);
  const dates = [
    ...new Set(
      scoped.map((trip) =>
        new Date(trip.scheduledTime).toISOString().slice(0, 10),
      ),
    ),
  ].sort();
  const visible = scoped.filter(
    (trip) =>
      `${trip.id} ${trip.routeNumber} ${trip.routeName} ${trip.vehicle} ${trip.driver} ${trip.bay}`
        .toLowerCase()
        .includes(query.toLowerCase()) &&
      (status === "All" || trip.status === status) &&
      (serviceDate === "All" ||
        new Date(trip.scheduledTime).toISOString().slice(0, 10) ===
          serviceDate),
  );
  function changeStatus(next: TripStatus) {
    if (selected) statusMutation.mutate({ id: selected.id, status: next });
  }

  if (isLoading)
    return (
      <main className="flex flex-1 items-center justify-center p-4">
        Loading dispatch board...
      </main>
    );
  if (error)
    return (
      <main className="p-4 text-red-600">Failed to load dispatch data.</main>
    );

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Dispatch control"
        description="Coordinate departures, bays, vehicles, and drivers at the selected centre."
        action={
          <div className="flex gap-2">
            <Button
              variant="outline"
              render={<Link to="/operations/history" />}
            >
              <History /> Trip history
            </Button>
            <Button render={<Link to="/operations/dispatch/new" />}>
              <Plus /> Schedule trip
            </Button>
          </div>
        }
      />
      <section className="flex flex-col gap-3 rounded-lg border bg-card p-3 xl:flex-row xl:items-center">
        <div className="relative min-w-64 flex-1">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder="Search trip, route, vehicle, driver, or bay"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </div>
        <Select
          value={serviceDate}
          onValueChange={(value) => setServiceDate(value ?? "All")}
        >
          <SelectTrigger className="min-w-40 bg-muted/60">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="All">All service dates</SelectItem>
            {dates.map((date) => (
              <SelectItem key={date} value={date}>
                {date}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={status}
          onValueChange={(value) => setStatus(value ?? "All")}
        >
          <SelectTrigger className="min-w-44 bg-muted/60">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="All">All active states</SelectItem>
            <SelectItem value="Delayed">Attention</SelectItem>
            <SelectItem value="Boarding">Boarding</SelectItem>
            <SelectItem value="Ready">Ready</SelectItem>
            <SelectItem value="Scheduled">Scheduled</SelectItem>
            <SelectItem value="Dispatched">Dispatched</SelectItem>
          </SelectContent>
        </Select>
        {selected && (
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              disabled={
                statusMutation.isPending || selected.status === "Dispatched"
              }
              onClick={() => changeStatus("Delayed")}
            >
              <TriangleAlert /> Flag delay
            </Button>
            <Button
              variant="outline"
              disabled={
                statusMutation.isPending ||
                !["Scheduled", "Ready", "Delayed"].includes(selected.status)
              }
              onClick={() => changeStatus("Boarding")}
            >
              Start boarding
            </Button>
            <Button
              disabled={
                statusMutation.isPending || selected.status !== "Boarding"
              }
              onClick={() => changeStatus("Dispatched")}
            >
              <CheckCircle2 /> Dispatch bus
            </Button>
          </div>
        )}
      </section>
      <DispatchBoard
        items={visible}
        selectedId={selectedId}
        onSelect={(trip) => setSelectedId(trip.id)}
      />
    </main>
  );
}
