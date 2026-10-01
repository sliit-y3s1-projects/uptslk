import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { PageHeading } from "@/components/shared/PageHeading";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { LabeledSelect } from "@/components/shared/LabeledSelect";
import { useAuth } from "@/hooks/useAuth";
import { Panel, QueryState, DataTable } from "./components/FeatureUi";
import { dateTime } from "./components/format";
import { useTrips, useManifest } from "@/features/fares/hooks/useBookings";
import { CentrePicker } from "@/features/fares/components/CentrePicker";

const PAGE_SIZE = 50;

export function PassengerFlowPage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const tripId = params.get("tripId") ?? "";
  const [selectedCentre, setCentre] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(0);
  const { centreId } = useCentreSelection(selectedCentre);
  const trips = useTrips(centreId);
  const manifest = useManifest(tripId);
  const bookings = useMemo(() => {
    const term = search.trim().toLowerCase();
    return [...(manifest.data?.bookings ?? [])]
      .filter((booking) => !status || booking.status === status)
      .filter(
        (booking) =>
          !term ||
          [booking.passenger, booking.phoneNumber, booking.qrCode].some(
            (value) => value?.toLowerCase().includes(term),
          ),
      );
  }, [manifest.data?.bookings, search, status]);
  const pageCount = Math.max(1, Math.ceil(bookings.length / PAGE_SIZE));
  const currentPage = Math.min(page, pageCount - 1);
  const visibleBookings = bookings.slice(
    currentPage * PAGE_SIZE,
    (currentPage + 1) * PAGE_SIZE,
  );
  const confirmedCount =
    manifest.data?.bookings.filter((booking) => booking.status === "Confirmed")
      .length ?? 0;

  function selectTrip(value: string) {
    setSearch("");
    setStatus("");
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
          className="rounded-xl border bg-card p-4"
        >
          <div
            className={`grid gap-3 ${user?.centreId ? "" : "lg:grid-cols-[240px_minmax(0,1fr)]"}`}
          >
            {!user?.centreId && (
              <CentrePicker
                selected={selectedCentre}
                onChange={(id) => {
                  setCentre(id);
                  selectTrip("");
                }}
              />
            )}
            <LabeledSelect
              label="Scheduled trip"
              value={
                trips.data?.some((trip) => trip.id === tripId) ? tripId : ""
              }
              onChange={selectTrip}
              disabled={!centreId || trips.isPending}
              placeholder="Choose a departure"
              options={(trips.data ?? []).map((trip) => ({
                value: trip.id,
                label: `${trip.routeNumber} · ${trip.routeName} · ${dateTime(trip.scheduledTime)} · ${trip.status}`,
              }))}
            />
          </div>
          {centreId && <QueryState query={trips} empty={!trips.data?.length} />}
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
                  <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_220px_auto]">
                    <div className="space-y-1.5">
                      <Label htmlFor="manifest-search">Search passengers</Label>
                      <Input
                        id="manifest-search"
                        value={search}
                        onChange={(event) => {
                          setSearch(event.target.value);
                          setPage(0);
                        }}
                        placeholder="Name, phone or booking reference"
                      />
                    </div>
                    <LabeledSelect
                      label="Booking status"
                      value={status || "all"}
                      onChange={(value) => {
                        setStatus(value === "all" ? "" : value);
                        setPage(0);
                      }}
                      options={[
                        { value: "all", label: "All statuses" },
                        ...[
                          "Pending",
                          "Confirmed",
                          "Completed",
                          "Cancelled",
                        ].map((value) => ({ value, label: value })),
                      ]}
                    />
                    <Button
                      className="self-end"
                      variant="ghost"
                      disabled={!search && !status}
                      onClick={() => {
                        setSearch("");
                        setStatus("");
                        setPage(0);
                      }}
                    >
                      Clear filters
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
      </div>
    </main>
  );
}

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
