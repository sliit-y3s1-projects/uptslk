import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/hooks/useAuth";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { usePassengers } from "@/features/riders/hooks/usePassengers";
import {
  Panel,
  Feedback,
  QueryState,
} from "@/features/riders/components/FeatureUi";
import { money, dateTime } from "@/features/riders/components/format";
import { useTrips, useSeats, useBookingMutations } from "../hooks/useBookings";
import { useFareQuote } from "../hooks/useFareRules";
import { SeatPicker } from "./SeatPicker";
export function CreateBooking() {
  const { user } = useAuth();
  const centreId = user?.centreId ?? "";
  const trips = useTrips(centreId);
  const passengers = usePassengers({ active: "true" });
  const [tripId, setTrip] = useState("");
  const [passengerId, setPassenger] = useState("");
  const [seatNumber, setSeat] = useState("");
  const quote = useFareQuote(tripId, passengerId);
  const seats = useSeats(tripId);
  const { checkout } = useBookingMutations();
  const trip = trips.data?.find((t) => t.id === tripId);
  const eligible =
    trip && ["Scheduled", "Ready", "Boarding"].includes(trip.status);
  return (
    <Panel title="New booking">
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          if (
            !eligible ||
            !quote.data ||
            seats.error ||
            !seats.data?.some(
              (s) => s.seatNumber === seatNumber && s.isAvailable,
            )
          )
            return;
          checkout.mutate(
            { tripId, passengerId, seatNumber },
            {
              onSuccess: (session) => {
                window.location.assign(session.url);
              },
            },
          );
        }}
      >
        <fieldset disabled={checkout.isPending} className="space-y-4">
          {!centreId && <p className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">Your account is not assigned to a centre, so a booking cannot be created.</p>}
          {centreId && (
            <QueryState
              query={trips}
              empty={
                !trips.data?.some((t) =>
                  ["Scheduled", "Ready", "Boarding"].includes(t.status),
                )
              }
            />
          )}
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="flex min-w-0 flex-col gap-1.5 text-sm">
              <label className="font-medium">Scheduled trip</label>
              <Select value={tripId || undefined} onValueChange={(value) => {
                setTrip(String(value ?? ""));
                setSeat("");
                checkout.reset();
              }}>
                <SelectTrigger className="h-11 w-full"><SelectValue placeholder="Select a trip" /></SelectTrigger>
                <SelectContent>{trips.data?.filter((tripOption) => ["Scheduled", "Ready", "Boarding"].includes(tripOption.status)).map((tripOption) => (
                  <SelectItem key={tripOption.id} value={tripOption.id}>
                    {tripOption.routeNumber} · {tripOption.routeName} · {dateTime(tripOption.scheduledTime)} · {tripOption.status}
                  </SelectItem>
                ))}</SelectContent>
              </Select>
            </div>
            <div className="flex min-w-0 flex-col gap-1.5 text-sm">
              <label className="font-medium">Passenger</label>
              <Select value={passengerId || undefined} onValueChange={(value) => {
                setPassenger(String(value ?? ""));
                checkout.reset();
              }}>
                <SelectTrigger className="h-11 w-full"><SelectValue placeholder="Select a passenger" /></SelectTrigger>
                <SelectContent>{passengers.data?.map((passenger) => <SelectItem key={passenger.id} value={passenger.id}>{passenger.fullName} · {passenger.phoneNumber} · {passenger.category}</SelectItem>)}</SelectContent>
              </Select>
            </div>
          </div>
          <QueryState query={passengers} empty={!passengers.data?.length} />
          {trip && (
            <p className="text-sm text-muted-foreground">
              Vehicle {trip.vehicle} · Bay {trip.bay}
            </p>
          )}
          {tripId && passengerId && (
            <>
              <QueryState query={quote} />
              {quote.data && !quote.error && (
                <p className="text-sm">
                  Fare: <strong>{money(quote.data.fare)}</strong> ? Wallet:{" "}
                  {money(
                    passengers.data?.find((p) => p.id === passengerId)
                      ?.balance ?? 0,
                  )}
                </p>
              )}
            </>
          )}
          {tripId && (
            <SeatPicker
              tripId={tripId}
              value={seatNumber}
              onChange={setSeat}
              disabled={checkout.isPending}
            />
          )}
          <Feedback error={checkout.error} />
          <Button
            type="submit"
            disabled={
              checkout.isPending ||
              !eligible ||
              !quote.data ||
              !!quote.error ||
              quote.isFetching ||
              !!seats.error ||
              !seats.data?.some(
                (s) => s.seatNumber === seatNumber && s.isAvailable,
              )
            }
          >
            {checkout.isPending
              ? "Opening secure checkout..."
              : seatNumber
                ? `Continue to payment for seat ${seatNumber}`
                : "Select a seat to continue"}
          </Button>
        </fieldset>
      </form>
    </Panel>
  );
}
