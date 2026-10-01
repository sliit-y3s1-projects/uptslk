import { useMemo, useState } from "react";
import { ArrowLeft, ChevronLeft, ChevronRight, Search } from "lucide-react";
import { Link, useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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
import { useTripHistory } from "./hooks/useTrips";

const PAGE_SIZE = 10;
const statusOptions = [
  { value: "all", label: "All statuses" },
  { value: "Completed", label: "Completed" },
  { value: "Cancelled", label: "Cancelled" },
];

export function TripHistoryPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { data: trips = [], isLoading, error } = useTripHistory(user?.centreId);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("all");
  const [page, setPage] = useState(1);

  const history = useMemo(() => {
    const term = query.trim().toLowerCase();
    return trips
      .filter((trip) => status === "all" || trip.status === status)
      .filter(
        (trip) =>
          !term ||
          `${trip.routeNumber} ${trip.routeName} ${trip.vehicle} ${trip.driver} ${trip.bay}`
            .toLowerCase()
            .includes(term),
      )
      .sort(
        (a, b) =>
          new Date(b.scheduledTime).getTime() -
          new Date(a.scheduledTime).getTime(),
      );
  }, [trips, query, status]);

  const pageCount = Math.max(1, Math.ceil(history.length / PAGE_SIZE));
  const currentPage = Math.min(page, pageCount);
  const start = (currentPage - 1) * PAGE_SIZE;
  const visible = history.slice(start, start + PAGE_SIZE);

  if (isLoading) return <main className="p-5">Loading trip history...</main>;
  if (error)
    return (
      <main className="p-5 text-red-600">Failed to load trip history.</main>
    );

  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Trip history"
        description="Completed and cancelled departures, newest first."
        action={
          <Button variant="outline" render={<Link to="/operations/dispatch" />}>
            <ArrowLeft /> Dispatch board
          </Button>
        }
      />

      <section className="overflow-hidden rounded-lg border bg-card">
        <div className="flex flex-col gap-2 border-b p-3 sm:flex-row sm:items-center">
          <div className="relative min-w-0 flex-1 sm:max-w-sm">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              className="pl-9"
              placeholder="Search route, vehicle, driver or bay"
              value={query}
              onChange={(event) => {
                setQuery(event.target.value);
                setPage(1);
              }}
            />
          </div>
          <Select
            value={status}
            onValueChange={(value) => {
              setStatus(String(value ?? "all"));
              setPage(1);
            }}
            itemToStringLabel={(value) =>
              statusOptions.find((item) => item.value === value)?.label ?? value
            }
          >
            <SelectTrigger className="min-w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {statusOptions.map((item) => (
                <SelectItem key={item.value} value={item.value}>
                  {item.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-sm text-muted-foreground sm:ml-auto">
            {history.length} trip{history.length === 1 ? "" : "s"}
          </p>
        </div>

        {visible.length === 0 ? (
          <p className="p-8 text-center text-sm text-muted-foreground">
            {trips.length === 0
              ? "No completed or cancelled trips yet."
              : "No trips match these filters."}
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/40 hover:bg-muted/40">
                <TableHead className="px-4">Departure</TableHead>
                <TableHead>Route</TableHead>
                <TableHead>Vehicle</TableHead>
                <TableHead>Driver</TableHead>
                <TableHead>Bay</TableHead>
                <TableHead>Passengers</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="px-4 text-right">Details</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visible.map((trip) => {
                const departure = new Date(trip.scheduledTime);
                return (
                  <TableRow
                    key={trip.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/operations/dispatch/${trip.id}`)}
                  >
                    <TableCell className="px-4">
                      <p className="font-medium">
                        {departure.toLocaleDateString([], {
                          day: "numeric",
                          month: "short",
                          year: "numeric",
                        })}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {departure.toLocaleTimeString([], {
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </p>
                    </TableCell>
                    <TableCell>
                      <p className="font-medium">Route {trip.routeNumber}</p>
                      <p className="text-xs text-muted-foreground">
                        {trip.routeName}
                      </p>
                    </TableCell>
                    <TableCell>{trip.vehicle}</TableCell>
                    <TableCell>{trip.driver}</TableCell>
                    <TableCell>{trip.bay}</TableCell>
                    <TableCell className="tabular-nums">
                      {trip.occupied} / {trip.capacity}
                    </TableCell>
                    <TableCell>
                      <StatusBadge
                        label={trip.status}
                        tone={trip.status === "Cancelled" ? "danger" : "good"}
                      />
                    </TableCell>
                    <TableCell className="px-4 text-right">
                      <Button
                        size="sm"
                        variant="outline"
                        render={<Link to={`/operations/dispatch/${trip.id}`} />}
                        onClick={(event) => event.stopPropagation()}
                      >
                        View
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        )}

        {history.length > PAGE_SIZE && (
          <footer className="flex items-center justify-between border-t px-4 py-3 text-sm">
            <span className="text-muted-foreground">
              Showing {start + 1}–{Math.min(start + PAGE_SIZE, history.length)}{" "}
              of {history.length}
            </span>
            <div className="flex items-center gap-2">
              <span className="text-muted-foreground">
                Page {currentPage} of {pageCount}
              </span>
              <Button
                size="icon"
                variant="outline"
                aria-label="Previous page"
                disabled={currentPage === 1}
                onClick={() => setPage(currentPage - 1)}
              >
                <ChevronLeft />
              </Button>
              <Button
                size="icon"
                variant="outline"
                aria-label="Next page"
                disabled={currentPage === pageCount}
                onClick={() => setPage(currentPage + 1)}
              >
                <ChevronRight />
              </Button>
            </div>
          </footer>
        )}
      </section>
    </main>
  );
}
