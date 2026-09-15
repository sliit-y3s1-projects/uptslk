import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { usePassengers } from "@/features/riders/hooks/usePassengers";
import {
  Panel,
  SelectField,
  Feedback,
  QueryState,
} from "@/features/riders/components/FeatureUi";
import { money, dateTime } from "@/features/riders/components/format";
import { useTrips, useSeats, useBookingMutations } from "../hooks/useBookings";
import { useFareQuote } from "../hooks/useFareRules";
import { CentrePicker } from "./CentrePicker";
import { SeatPicker } from "./SeatPicker";
export function CreateBooking({
  onCreated,
}: {
  onCreated: (id: string) => void;
}) {
  const [selectedCentre, setCentre] = useState("");
  const { centreId } = useCentreSelection(selectedCentre);
  const trips = useTrips(centreId);
  const passengers = usePassengers({ active: "true" });
  const [tripId, setTrip] = useState("");
  const [passengerId, setPassenger] = useState("");
  const [seatNumber, setSeat] = useState("");
  const quote = useFareQuote(tripId, passengerId);
  const seats = useSeats(tripId);
  const { create } = useBookingMutations();
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
          create.mutate(
            { tripId, passengerId, seatNumber },
            { onSuccess: (data) => onCreated(data.id) },
          );
        }}
      >
        <fieldset disabled={create.isPending} className="space-y-4">
          <CentrePicker
            selected={selectedCentre}
            onChange={(id) => {
              setCentre(id);
              setTrip("");
              setSeat("");
              create.reset();
            }}
          />
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
            <SelectField
              label="Scheduled trip"
              required
              value={tripId}
              onChange={(e) => {
                setTrip(e.target.value);
                setSeat("");
                create.reset();
              }}
            >
              <option value="">Select a trip</option>
              {trips.data
                ?.filter((t) =>
                  ["Scheduled", "Ready", "Boarding"].includes(t.status),
                )
                .map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.routeNumber} · {t.routeName} ?{" "}
                    {dateTime(t.scheduledTime)} · {t.status}
                  </option>
                ))}
            </SelectField>
            <SelectField
              label="Passenger"
              required
              value={passengerId}
              onChange={(e) => {
                setPassenger(e.target.value);
                create.reset();
              }}
            >
              <option value="">Select a passenger</option>
              {passengers.data?.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName} · {p.phoneNumber} · {p.category}
                </option>
              ))}
            </SelectField>
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
              disabled={create.isPending}
            />
          )}
          <Feedback error={create.error} />
          <Button
            type="submit"
            disabled={
              create.isPending ||
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
            {create.isPending
              ? "Confirming..."
              : seatNumber
                ? `Confirm seat ${seatNumber} and pay`
                : "Select a seat to continue"}
          </Button>
        </fieldset>
      </form>
    </Panel>
  );
}
