import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useState } from "react";
import { useSearchParams } from "react-router";
import { Button } from "@/components/ui/button";
import { PageHeading } from "@/components/shared/PageHeading";
import { PassengerWorkspace } from "@/features/riders/RiderPages";
import {
  Panel,
  SelectField,
  QueryState,
  DataTable,
  Feedback,
} from "@/features/riders/components/FeatureUi";
import { money, dateTime } from "@/features/riders/components/format";
import { usePassengers } from "@/features/riders/hooks/usePassengers";
import { useBookings, useTrips } from "./hooks/useBookings";
import { CreateBooking } from "./components/CreateBooking";
import { BookingTicket } from "./components/BookingTicket";
import { FareRules } from "./components/FareRules";
import { CentrePicker } from "./components/CentrePicker";
export function TicketsPage() {
  const [params, setParams] = useSearchParams();
  const id = params.get("bookingId") ?? "";
  const [view, setView] = useState("bookings");
  const [adding, setAdding] = useState(false);
  const [passengerId, setPassenger] = useState("");
  const [tripId, setTrip] = useState("");
  const [status, setStatus] = useState("");
  const [selectedCentre, setCentre] = useState("");
  const { centreId } = useCentreSelection(selectedCentre);
  const trips = useTrips(centreId);
  const passengers = usePassengers();
  const query = useBookings({ passengerId, tripId, status });
  const [notice, setNotice] = useState("");
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Bookings & fares"
        description="Manage passenger tickets, seat assignments, fare rules and refunds."
      />
      <nav aria-label="Bookings and fares" className="flex gap-2 border-b pb-3">
        <Button
          variant={view === "bookings" ? "default" : "ghost"}
          aria-pressed={view === "bookings"}
          onClick={() => setView("bookings")}
        >
          Bookings & tickets
        </Button>
        <Button
          variant={view === "fares" ? "default" : "ghost"}
          aria-pressed={view === "fares"}
          onClick={() => setView("fares")}
        >
          Fare rules
        </Button>
      </nav>
      {view === "fares" ? (
        <FareRules />
      ) : (
        <>
          <Feedback success={notice} />
          <div>
            <Button onClick={() => setAdding(!adding)}>
              {adding ? "Close new booking" : "Create booking"}
            </Button>
          </div>
          {adding && (
            <CreateBooking
              onCreated={(newId) => {
                setAdding(false);
                setParams({ bookingId: newId });
                setNotice(
                  "Booking confirmed. Fare charged to the passenger wallet.",
                );
              }}
            />
          )}
          {id && (
            <>
              <div>
                <Button variant="ghost" onClick={() => setParams({})}>
                  Close ticket
                </Button>
              </div>
              <BookingTicket key={id} id={id} />
            </>
          )}
          <Panel title="Booking records">
            <CentrePicker
              selected={selectedCentre}
              onChange={(value) => {
                setCentre(value);
                setTrip("");
              }}
            />
            <div className="grid gap-3 sm:grid-cols-3">
              <SelectField
                label="Passenger filter"
                value={passengerId}
                onChange={(e) => setPassenger(e.target.value)}
              >
                <option value="">All passengers</option>
                {passengers.data?.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.fullName} · {p.phoneNumber}
                  </option>
                ))}
              </SelectField>
              <SelectField
                label="Trip filter"
                value={tripId}
                onChange={(e) => setTrip(e.target.value)}
              >
                <option value="">All trips</option>
                {trips.data?.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.routeNumber} · {dateTime(t.scheduledTime)}
                  </option>
                ))}
              </SelectField>
              <SelectField
                label="Booking status"
                value={status}
                onChange={(e) => setStatus(e.target.value)}
              >
                <option value="">All statuses</option>
                {["Pending", "Confirmed", "Completed", "Cancelled"].map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </SelectField>
            </div>
            <QueryState query={passengers} />
            {centreId && <QueryState query={trips} />}
            <QueryState query={query} empty={!query.data?.length} />
            {query.data && !query.error && (
              <DataTable
                headings={[
                  "Passenger",
                  "Route / departure",
                  "Seat",
                  "Fare",
                  "Status",
                  "Ticket",
                ]}
              >
                {query.data.map((b) => (
                  <tr key={b.id}>
                    <td className="font-medium">{b.passenger}</td>
                    <td>
                      {b.route}
                      <p className="text-xs text-muted-foreground">
                        {dateTime(b.tripTime)}
                      </p>
                    </td>
                    <td>{b.seatNumber}</td>
                    <td>{money(b.fare)}</td>
                    <td>{b.status}</td>
                    <td>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setParams({ bookingId: b.id })}
                      >
                        View ticket
                      </Button>
                    </td>
                  </tr>
                ))}
              </DataTable>
            )}
          </Panel>
        </>
      )}
    </main>
  );
}
export function PaymentsPage() {
  return (
    <PassengerWorkspace
      title="Wallets & payments"
      description="Select a passenger to top up their wallet and review fare charges and refunds."
    />
  );
}
