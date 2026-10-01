import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { SlidersHorizontal, Search, X } from "lucide-react";
import { PageHeading } from "@/components/shared/PageHeading";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { DatePicker } from "@/components/custom/DatePicker";
import { LabeledSelect } from "@/components/shared/LabeledSelect";
import { useAuth } from "@/hooks/useAuth";
import { Panel, QueryState, DataTable } from "./components/FeatureUi";
import { dateTime } from "./components/format";
import {
  useTrips,
  useManifest,
  useRoutes,
} from "@/features/fares/hooks/useBookings";
import { CentrePicker } from "@/features/fares/components/CentrePicker";

const PAGE_SIZE = 50;

export function PassengerFlowPage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const tripId = params.get("tripId") ?? "";
  const [selectedCentre, setCentre] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [category, setCategory] = useState("");
  const [routeId, setRouteId] = useState("");
  const [date, setDate] = useState("");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [page, setPage] = useState(0);
  const { centreId } = useCentreSelection(selectedCentre);
  const routes = useRoutes(centreId);
  const allTrips = useTrips(centreId);
  const trips = useMemo(
    () =>
      (allTrips.data ?? []).filter(
        (trip) =>
          (!routeId || trip.routeId === routeId) &&
          (!date || localDate(trip.scheduledTime) === date),
      ),
    [allTrips.data, routeId, date],
  );
  const manifest = useManifest(tripId);
  const bookings = useMemo(() => {
    const term = search.trim().toLowerCase();
    return [...(manifest.data?.bookings ?? [])]
      .filter((booking) => !status || booking.status === status)
      .filter((booking) => !category || booking.category === category)
      .filter(
        (booking) =>
          !term ||
          [booking.passenger, booking.phoneNumber, booking.qrCode].some(
            (value) => value?.toLowerCase().includes(term),
          ),
      );
  }, [manifest.data?.bookings, search, status, category]);
  const pageCount = Math.max(1, Math.ceil(bookings.length / PAGE_SIZE));
  const currentPage = Math.min(page, pageCount - 1);
  const visibleBookings = bookings.slice(
    currentPage * PAGE_SIZE,
    (currentPage + 1) * PAGE_SIZE,
  );
  const categories = [
    ...new Set((manifest.data?.bookings ?? []).map((item) => item.category)),
  ].filter(Boolean);
  const appliedFilterCount = [status, category].filter(Boolean).length;
  const confirmedCount =
    manifest.data?.bookings.filter((booking) => booking.status === "Confirmed")
      .length ?? 0;

  function selectTrip(value: string) {
    setSearch("");
    setStatus("");
    setCategory("");
    setPage(0);
    setParams(value ? { tripId: value } : {});
  }

  return (
    <main className="flex flex-1 flex-col gap-5 bg-muted/20 p-4 lg:p-6">
      <div className="mx-auto flex w-full max-w-[1600px] flex-col gap-5">
        <PageHeading
          title="Passenger flow"
          description="Review passenger lists and boarding details for each departure."
        />
        <section
          aria-label="Departure selection"
          className="space-y-3 rounded-xl border bg-card p-4"
        >
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            {!user?.centreId && (
              <CentrePicker
                selected={selectedCentre}
                onChange={(id) => {
                  setCentre(id);
                  setRouteId("");
                  selectTrip("");
                }}
              />
            )}
            <LabeledSelect
              label="Route"
              value={routeId || "all"}
              onChange={(value) => {
                setRouteId(value === "all" ? "" : value);
                selectTrip("");
              }}
              disabled={!centreId || routes.isPending}
              options={[
                { value: "all", label: "All routes" },
                ...(routes.data ?? []).map((route) => ({
                  value: route.id,
                  label: `${route.routeNumber} · ${route.name}`,
                })),
              ]}
            />
            <div className="min-w-0 space-y-1.5">
              <Label>Date</Label>
              <div className="flex gap-2">
                <DatePicker
                  name="tripDate"
                  className="h-8 min-w-0 flex-1"
                  value={date}
                  onValueChange={(value) => {
                    setDate(value);
                    selectTrip("");
                  }}
                />
                {date && (
                  <Button
                    type="button"
                    size="icon"
                    variant="outline"
                    className="size-8 shrink-0"
                    aria-label="Clear date"
                    onClick={() => {
                      setDate("");
                      selectTrip("");
                    }}
                  >
                    <X />
                  </Button>
                )}
              </div>
            </div>
            <LabeledSelect
              label={`Departure${centreId ? ` (${trips.length})` : ""}`}
              value={trips.some((trip) => trip.id === tripId) ? tripId : ""}
              onChange={selectTrip}
              disabled={!centreId || allTrips.isPending}
              placeholder={
                centreId && !trips.length && !allTrips.isPending
                  ? "No departures match"
                  : "Choose a departure"
              }
              options={trips.map((trip) => ({
                value: trip.id,
                label: `${trip.routeNumber} · ${dateTime(trip.scheduledTime)} · ${trip.status}`,
              }))}
            />
          </div>
          {centreId && (
            <QueryState query={allTrips} empty={!allTrips.data?.length} />
          )}
        </section>
        {!tripId && (
          <p className="rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
            Choose a departure to view its passenger manifest.
          </p>
        )}

        {tripId && (
          <>
            <QueryState query={manifest} />
            {manifest.data && !manifest.error && (
              <>
                <section className="grid gap-3 sm:grid-cols-3">
                  <FlowStat
                    label="Passengers on manifest"
                    value={manifest.data.passengerCount}
                    detail={`${confirmedCount} confirmed`}
                  />
                  <FlowStat
                    label="Capacity"
                    value={manifest.data.trip.capacity}
                    detail={`${manifest.data.passengerCount} spaces currently booked`}
                  />
                  <FlowStat
                    label="Departure"
                    value={dateTime(manifest.data.trip.scheduledTime)}
                    detail={`${manifest.data.trip.route} · ${manifest.data.trip.vehicle}`}
                    compact
                  />
                </section>
                <Panel title="Passenger manifest">
                  <div className="flex flex-col gap-4 border-b pb-4 lg:flex-row lg:items-end lg:justify-between">
                    <div>
                      <h3 className="font-medium">
                        {manifest.data.trip.route} · {manifest.data.trip.name}
                      </h3>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Updates every 15 seconds.
                      </p>
                    </div>
                    <Button
                      variant="outline"
                      disabled={manifest.isFetching}
                      onClick={() => {
                        void manifest.refetch();
                      }}
                    >
                      Refresh manifest
                    </Button>
                  </div>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <div className="relative min-w-0 flex-1">
                      <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        id="manifest-search"
                        aria-label="Search passengers"
                        className="h-10 pl-9"
                        value={search}
                        onChange={(event) => {
                          setSearch(event.target.value);
                          setPage(0);
                        }}
                        placeholder="Search name, phone or booking reference"
                      />
                    </div>
                    <Button
                      variant="outline"
                      className="h-10 gap-2"
                      onClick={() => setFiltersOpen(true)}
                    >
                      <SlidersHorizontal />
                      Filters
                      {appliedFilterCount > 0 && (
                        <span className="rounded-full bg-primary px-1.5 text-xs text-primary-foreground">
                          {appliedFilterCount}
                        </span>
                      )}
                    </Button>
                  </div>
                  <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
                    <p>
                      {bookings.length.toLocaleString()} matching passenger
                      {bookings.length === 1 ? "" : "s"}
                    </p>
                    <p>
                      Showing{" "}
                      {bookings.length
                        ? `${currentPage * PAGE_SIZE + 1}–${Math.min((currentPage + 1) * PAGE_SIZE, bookings.length)}`
                        : "0"}
                    </p>
                  </div>
                  {bookings.length ? (
                    <DataTable
                      headings={[
                        "Passenger",
                        "Count",
                        "Contact",
                        "Category",
                        "Status",
                        "QR reference",
                        "Action",
                      ]}
                    >
                      {visibleBookings.map((booking) => (
                        <tr key={booking.id}>
                          <td className="font-medium">{booking.passenger}</td>
                          <td className="font-semibold">
                            {booking.passengerCount}
                          </td>
                          <td>{booking.phoneNumber}</td>
                          <td>{booking.category}</td>
                          <td>
                            <StatusPill status={booking.status} />
                          </td>
                          <td
                            className="max-w-36 truncate font-mono text-xs"
                            title={booking.qrCode}
                          >
                            {booking.qrCode}
                          </td>
                          <td>
                            <Link
                              to={`/fares/bookings?bookingId=${booking.id}`}
                            >
                              <Button size="sm" variant="outline">
                                View ticket
                              </Button>
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </DataTable>
                  ) : (
                    <p className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
                      No passengers match these filters.
                    </p>
                  )}
                  {bookings.length > PAGE_SIZE && (
                    <div className="flex items-center justify-end gap-3">
                      <span className="text-sm text-muted-foreground">
                        Page {currentPage + 1} of {pageCount}
                      </span>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={currentPage === 0}
                        onClick={() => setPage(currentPage - 1)}
                      >
                        Previous
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={currentPage >= pageCount - 1}
                        onClick={() => setPage(currentPage + 1)}
                      >
                        Next
                      </Button>
                    </div>
                  )}
                </Panel>
              </>
            )}
          </>
        )}
        <Sheet open={filtersOpen} onOpenChange={setFiltersOpen}>
          <SheetContent side="right" className="sm:!max-w-sm">
            <SheetHeader>
              <SheetTitle>Filter passengers</SheetTitle>
              <SheetDescription>
                Narrow the manifest by booking status or passenger category.
              </SheetDescription>
            </SheetHeader>
            <div className="space-y-5 py-2">
              <LabeledSelect
                label="Booking status"
                value={status || "all"}
                onChange={(value) => {
                  setStatus(value === "all" ? "" : value);
                  setPage(0);
                }}
                options={[
                  { value: "all", label: "All statuses" },
                  ...["Pending", "Confirmed", "Completed", "Cancelled"].map(
                    (value) => ({ value, label: value }),
                  ),
                ]}
              />
              <LabeledSelect
                label="Passenger category"
                value={category || "all"}
                onChange={(value) => {
                  setCategory(value === "all" ? "" : value);
                  setPage(0);
                }}
                options={[
                  { value: "all", label: "All categories" },
                  ...categories.map((value) => ({ value, label: value })),
                ]}
              />
            </div>
            <SheetFooter>
              <Button
                variant="outline"
                disabled={!status && !category}
                onClick={() => {
                  setStatus("");
                  setCategory("");
                  setPage(0);
                }}
              >
                Clear filters
              </Button>
              <Button onClick={() => setFiltersOpen(false)}>Done</Button>
            </SheetFooter>
          </SheetContent>
        </Sheet>
      </div>
    </main>
  );
}

const localDate = (value: string) => {
  const day = new Date(value);
  return `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, "0")}-${String(day.getDate()).padStart(2, "0")}`;
};

function FlowStat({
  label,
  value,
  detail,
  compact = false,
}: {
  label: string;
  value: string | number;
  detail: string;
  compact?: boolean;
}) {
  return (
    <section className="rounded-xl border bg-card p-4">
      <p className="text-sm text-muted-foreground">{label}</p>
      <p
        className={
          compact
            ? "mt-1 text-base font-semibold"
            : "mt-1 text-2xl font-semibold"
        }
      >
        {value}
      </p>
      <p className="mt-1 truncate text-xs text-muted-foreground" title={detail}>
        {detail}
      </p>
    </section>
  );
}

function StatusPill({ status }: { status: string }) {
  const style =
    status === "Confirmed"
      ? "bg-emerald-100 text-emerald-800"
      : status === "Pending"
        ? "bg-amber-100 text-amber-800"
        : status === "Cancelled"
          ? "bg-rose-100 text-rose-800"
          : "bg-slate-100 text-slate-700";
  return (
    <span className={`rounded-full px-2 py-1 text-xs font-medium ${style}`}>
      {status}
    </span>
  );
}
