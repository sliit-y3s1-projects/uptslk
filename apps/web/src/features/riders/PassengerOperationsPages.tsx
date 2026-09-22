import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { PageHeading } from "@/components/shared/PageHeading";
import { Button } from "@/components/ui/button";
import { Panel, SelectField, QueryState, DataTable, Field } from "./components/FeatureUi";
import { dateTime } from "./components/format";
import { useTrips, useManifest, useSeats } from "@/features/fares/hooks/useBookings";
import { CentrePicker } from "@/features/fares/components/CentrePicker";
import { RequestQueue } from "./RequestQueue";

const PAGE_SIZE = 50;

export function PassengerFlowPage() {
  const [params, setParams] = useSearchParams();
  const tripId = params.get("tripId") ?? "";
  const [selectedCentre, setCentre] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(0);
  const { centreId } = useCentreSelection(selectedCentre);
  const trips = useTrips(centreId);
  const manifest = useManifest(tripId);
  const seats = useSeats(tripId);
  const bookings = useMemo(() => {
    const term = search.trim().toLowerCase();
    return [...(manifest.data?.bookings ?? [])]
      .sort((a, b) => Number(a.seatNumber) - Number(b.seatNumber))
      .filter((booking) => !status || booking.status === status)
      .filter((booking) => !term || [booking.passenger, booking.phoneNumber, booking.seatNumber, booking.qrCode].some((value) => value?.toLowerCase().includes(term)));
  }, [manifest.data?.bookings, search, status]);
  const pageCount = Math.max(1, Math.ceil(bookings.length / PAGE_SIZE));
  const visibleBookings = bookings.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);
  const confirmedCount = manifest.data?.bookings.filter((booking) => booking.status === "Confirmed").length ?? 0;
  const availableSeats = seats.data?.filter((seat) => seat.isAvailable).length;

  function selectTrip(value: string) {
    setSearch("");
    setStatus("");
    setPage(0);
    setParams(value ? { tripId: value } : {});
  }

  return (
    <main className="flex flex-1 flex-col gap-5 bg-muted/20 p-4 lg:p-6">
      <div className="mx-auto flex w-full max-w-[1600px] flex-col gap-5">
        <PageHeading title="Passenger flow" description="Find a trip, review its manifest and prepare boarding without loading a long unfiltered list." />
        <Panel title="Choose a departure">
          <div className="grid gap-4 lg:grid-cols-2">
            <CentrePicker selected={selectedCentre} onChange={(id) => { setCentre(id); selectTrip(""); }} />
            <SelectField label="Scheduled trip" value={trips.data?.some((trip) => trip.id === tripId) ? tripId : ""} onChange={(event) => selectTrip(event.target.value)}>
              <option value="">Select a trip to view its manifest</option>
              {trips.data?.map((trip) => <option key={trip.id} value={trip.id}>{trip.routeNumber} · {trip.routeName} · {dateTime(trip.scheduledTime)} · {trip.status}</option>)}
            </SelectField>
          </div>
          {centreId && <QueryState query={trips} empty={!trips.data?.length} />}
        </Panel>

        {tripId && <>
          <QueryState query={manifest} />
          <QueryState query={seats} />
          {manifest.data && !manifest.error && <>
            <section className="grid gap-3 sm:grid-cols-3">
              <FlowStat label="Passengers on manifest" value={manifest.data.passengerCount} detail={`${confirmedCount} confirmed`} />
              <FlowStat label="Seats available" value={availableSeats ?? "—"} detail={seats.data ? `${seats.data.length} total seats` : "Loading seat availability"} />
              <FlowStat label="Departure" value={dateTime(manifest.data.trip.scheduledTime)} detail={`${manifest.data.trip.route} · ${manifest.data.trip.vehicle}`} compact />
            </section>
            <Panel title="Passenger manifest">
              <div className="flex flex-col gap-4 border-b pb-4 lg:flex-row lg:items-end lg:justify-between">
                <div><h3 className="font-medium">{manifest.data.trip.route} · {manifest.data.trip.name}</h3><p className="mt-1 text-sm text-muted-foreground">Search or filter before issuing a ticket action. The live manifest refreshes every 15 seconds.</p></div>
                <Button variant="outline" disabled={manifest.isFetching || seats.isFetching} onClick={() => { void manifest.refetch(); void seats.refetch(); }}>Refresh manifest</Button>
              </div>
              <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_220px_auto]">
                <Field label="Search passenger, phone, seat or QR reference" value={search} onChange={(event) => { setSearch(event.target.value); setPage(0); }} placeholder="Search manifest" />
                <SelectField label="Booking status" value={status} onChange={(event) => { setStatus(event.target.value); setPage(0); }}><option value="">All statuses</option>{["Pending", "Confirmed", "Completed", "Cancelled"].map((value) => <option key={value}>{value}</option>)}</SelectField>
                <Button className="self-end" variant="ghost" disabled={!search && !status} onClick={() => { setSearch(""); setStatus(""); setPage(0); }}>Clear filters</Button>
              </div>
              <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground"><p>{bookings.length.toLocaleString()} matching passenger{bookings.length === 1 ? "" : "s"}</p><p>Showing {bookings.length ? `${page * PAGE_SIZE + 1}–${Math.min((page + 1) * PAGE_SIZE, bookings.length)}` : "0"}</p></div>
              {bookings.length ? <DataTable headings={["Seat", "Passenger", "Contact", "Category", "Status", "QR reference", "Action"]}>{visibleBookings.map((booking) => <tr key={booking.id}><td className="font-semibold">{booking.seatNumber}</td><td className="font-medium">{booking.passenger}</td><td>{booking.phoneNumber}</td><td>{booking.category}</td><td><StatusPill status={booking.status} /></td><td className="max-w-36 truncate font-mono text-xs" title={booking.qrCode}>{booking.qrCode}</td><td><Link to={`/fares/bookings?bookingId=${booking.id}`}><Button size="sm" variant="outline">View ticket</Button></Link></td></tr>)}</DataTable> : <p className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">No passengers match these filters.</p>}
              {bookings.length > PAGE_SIZE && <div className="flex items-center justify-end gap-3"><span className="text-sm text-muted-foreground">Page {page + 1} of {pageCount}</span><Button size="sm" variant="outline" disabled={page === 0} onClick={() => setPage((current) => current - 1)}>Previous</Button><Button size="sm" variant="outline" disabled={page >= pageCount - 1} onClick={() => setPage((current) => current + 1)}>Next</Button></div>}
            </Panel>
          </>}
        </>}
      </div>
    </main>
  );
}

function FlowStat({ label, value, detail, compact = false }: { label: string; value: string | number; detail: string; compact?: boolean }) {
  return <section className="rounded-xl border bg-card p-4"><p className="text-sm text-muted-foreground">{label}</p><p className={compact ? "mt-1 text-base font-semibold" : "mt-1 text-2xl font-semibold"}>{value}</p><p className="mt-1 truncate text-xs text-muted-foreground" title={detail}>{detail}</p></section>;
}

function StatusPill({ status }: { status: string }) {
  const style = status === "Confirmed" ? "bg-emerald-100 text-emerald-800" : status === "Pending" ? "bg-amber-100 text-amber-800" : status === "Cancelled" ? "bg-rose-100 text-rose-800" : "bg-slate-100 text-slate-700";
  return <span className={`rounded-full px-2 py-1 text-xs font-medium ${style}`}>{status}</span>;
}

export function AssistancePage() {
  return <RequestQueue type="Assistance" title="Passenger assistance" description="Track accessibility and journey assistance from request to resolution." />;
}
