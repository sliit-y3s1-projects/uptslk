import * as React from "react";
import { Link } from "react-router";
import { UsersRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DatePicker } from "@/components/custom/DatePicker";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PageHeading } from "@/components/shared/PageHeading";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { useAuth } from "@/hooks/useAuth";
import { useTrips } from "./hooks/useTrips";

const today = new Date().toISOString().slice(0, 10);

export function DutyRosterPage() {
  const { user } = useAuth();
  const [date, setDate] = React.useState(today);
  const [search, setSearch] = React.useState("");
  const {
    data: trips = [],
    isLoading,
    error,
  } = useTrips({ centreId: user?.centreId, date });
  const activeTrips = React.useMemo(
    () =>
      trips
        .filter((trip) => !["Cancelled", "Completed"].includes(trip.status))
        .sort(
          (left, right) =>
            left.driver.localeCompare(right.driver) ||
            new Date(left.scheduledTime).getTime() -
              new Date(right.scheduledTime).getTime(),
        ),
    [trips],
  );
  const filteredTrips = React.useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return activeTrips;
    return activeTrips.filter((trip) =>
      [
        trip.driver,
        trip.routeNumber,
        trip.routeName,
        trip.destination,
        trip.vehicle,
        trip.bay,
      ].some((value) => value?.toLowerCase().includes(term)),
    );
  }, [activeTrips, search]);
  const assignedDrivers = new Set(
    activeTrips.filter((trip) => trip.driverId).map((trip) => trip.driverId),
  ).size;
  const unassignedTrips = activeTrips.filter(
    (trip) => !trip.driverId || !trip.vehicleId || !trip.bayId,
  ).length;

  return (
    <main className="flex flex-1 flex-col gap-5 bg-muted/20 p-4">
      <PageHeading
        title="Daily duty roster"
        description="Review the bus and departure duties assigned to each driver. Reassign a trip from Dispatch when the planned duty changes."
        action={
          <Button
            variant="outline"
            render={<Link to={`/operations/dispatch?date=${date}`} />}
          >
            Open dispatch board
          </Button>
        }
      />
      <section className="rounded-xl border border-slate-300 bg-card">
        <div className="grid gap-4 border-b border-slate-200 p-4 lg:grid-cols-[minmax(220px,280px)_minmax(260px,1fr)_auto] lg:items-end">
          <div>
            <p className="text-sm font-medium">Service date</p>
            <div className="mt-2">
              <DatePicker
                name="rosterDate"
                value={date}
                onValueChange={setDate}
              />
            </div>
          </div>
          <div>
            <label htmlFor="roster-search" className="text-sm font-medium">
              Find a duty
            </label>
            <Input
              id="roster-search"
              className="mt-2"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search driver, route, vehicle, or bay"
            />
          </div>
          <dl className="grid grid-cols-3 gap-5 rounded-xl bg-slate-50 px-4 py-3 text-sm">
            <div>
              <dt className="text-muted-foreground">Drivers</dt>
              <dd className="mt-1 text-lg font-semibold">{assignedDrivers}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Departures</dt>
              <dd className="mt-1 text-lg font-semibold">
                {activeTrips.length}
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Unassigned</dt>
              <dd
                className={`mt-1 text-lg font-semibold ${
                  unassignedTrips ? "text-amber-700" : "text-emerald-700"
                }`}
              >
                {unassignedTrips}
              </dd>
            </div>
          </dl>
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Spinner /> Loading duties
          </div>
        ) : error ? (
          <p className="m-4 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
            Could not load the duty roster.
          </p>
        ) : activeTrips.length === 0 ? (
          <div className="p-10 text-center">
            <UsersRound className="mx-auto size-8 text-muted-foreground" />
            <h2 className="mt-3 font-semibold">No duties scheduled</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Generate timetable trips, then assign their bus and driver in
              Dispatch.
            </p>
          </div>
        ) : filteredTrips.length === 0 ? (
          <div className="p-10 text-center">
            <h2 className="font-semibold">No matching duties</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Try another driver, route, vehicle, or bay.
            </p>
          </div>
        ) : (
          <Table>
            <TableHeader className="bg-slate-50/80">
              <TableRow>
                <TableHead className="pl-5">Driver</TableHead>
                <TableHead>Departure</TableHead>
                <TableHead>Route and direction</TableHead>
                <TableHead>Vehicle</TableHead>
                <TableHead>Bay</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="pr-5 text-right">Trip</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredTrips.map((trip) => (
                <TableRow key={trip.id}>
                  <TableCell className="pl-5 font-medium">
                    {trip.driver || "Not assigned"}
                  </TableCell>
                  <TableCell className="font-semibold">
                    {new Date(trip.scheduledTime).toLocaleTimeString([], {
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </TableCell>
                  <TableCell>
                    <p className="font-medium">{trip.routeNumber}</p>
                    <p className="mt-0.5 max-w-72 truncate text-xs text-muted-foreground">
                      {trip.directionName ?? trip.destination ?? trip.routeName}
                    </p>
                  </TableCell>
                  <TableCell>{trip.vehicle || "Not assigned"}</TableCell>
                  <TableCell>{trip.bay || "Not assigned"}</TableCell>
                  <TableCell>
                    <StatusBadge
                      label={trip.status}
                      tone={
                        trip.status === "Delayed"
                          ? "danger"
                          : trip.status === "Boarding"
                            ? "warning"
                            : "good"
                      }
                    />
                  </TableCell>
                  <TableCell className="pr-5 text-right">
                    <Button
                      size="sm"
                      variant="outline"
                      render={<Link to={`/operations/dispatch/${trip.id}`} />}
                    >
                      View
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>
    </main>
  );
}
