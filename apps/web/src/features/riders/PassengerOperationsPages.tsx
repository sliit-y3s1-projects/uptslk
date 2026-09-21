import { useCentreSelection } from "@/features/fares/hooks/useCentreSelection";
import { useState } from "react";
import { Link, useSearchParams } from "react-router";
import { PageHeading } from "@/components/shared/PageHeading";
import { Button } from "@/components/ui/button";
import {
  Panel,
  SelectField,
  QueryState,
  DataTable,
} from "./components/FeatureUi";
import { dateTime } from "./components/format";
import {
  useTrips,
  useManifest,
  useSeats,
} from "@/features/fares/hooks/useBookings";
import { CentrePicker } from "@/features/fares/components/CentrePicker";
import { RequestQueue } from "./RequestQueue";
export function PassengerFlowPage() {
  const [params, setParams] = useSearchParams();
  const tripId = params.get("tripId") ?? "";
  const [selectedCentre, setCentre] = useState("");
  const { centreId } = useCentreSelection(selectedCentre);
  const trips = useTrips(centreId);
  const manifest = useManifest(tripId);
  const seats = useSeats(tripId);
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading
        title="Passenger flow"
        description="Review trip seat availability and the passenger manifest before boarding."
      />
      <Panel title="Select a trip">
        <CentrePicker
          selected={selectedCentre}
          onChange={(id) => {
            setCentre(id);
            setParams({});
          }}
        />
        <SelectField
          label="Trip"
          value={trips.data?.some((t) => t.id === tripId) ? tripId : ""}
          onChange={(e) =>
            setParams(e.target.value ? { tripId: e.target.value } : {})
          }
        >
          <option value="">Select a trip</option>
          {trips.data?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.routeNumber} · {t.routeName} · {dateTime(t.scheduledTime)} ·{" "}
              {t.status}
            </option>
          ))}
        </SelectField>
        {centreId && <QueryState query={trips} empty={!trips.data?.length} />}
      </Panel>
      {tripId && (
        <Panel title="Passenger manifest">
          <div className="flex justify-end">
            <Button
              variant="outline"
              disabled={manifest.isFetching || seats.isFetching}
              onClick={() => {
                void manifest.refetch();
                void seats.refetch();
              }}
            >
              Refresh manifest
            </Button>
          </div>
          <QueryState query={manifest} />
          <QueryState query={seats} />
          {manifest.data && !manifest.error && (
            <>
              <h3 className="font-medium">
                {manifest.data.trip.route} · {manifest.data.trip.name}
              </h3>
              <p className="text-sm text-muted-foreground">
                {dateTime(manifest.data.trip.scheduledTime)} · Vehicle{" "}
                {manifest.data.trip.vehicle}
              </p>
              <p className="text-sm">
                {manifest.data.passengerCount} passengers on manifest
                {seats.data &&
                  !seats.error &&
                  ` · ${seats.data.filter((s) => s.isAvailable).length} available of ${seats.data.length} seats`}
              </p>
              <DataTable
                headings={[
                  "Seat",
                  "Passenger",
                  "Phone",
                  "Category",
                  "Status",
                  "QR reference",
                  "Ticket",
                ]}
              >
                {[...manifest.data.bookings]
                  .sort((a, b) => Number(a.seatNumber) - Number(b.seatNumber))
                  .map((b) => (
                    <tr key={b.id}>
                      <td>{b.seatNumber}</td>
                      <td className="font-medium">{b.passenger}</td>
                      <td>{b.phoneNumber}</td>
                      <td>{b.category}</td>
                      <td>{b.status}</td>
                      <td className="font-mono text-xs">{b.qrCode}</td>
                      <td>
                        <Link
                          className="underline"
                          to={`/fares/tickets?bookingId=${b.id}`}
                        >
                          View ticket
                        </Link>
                      </td>
                    </tr>
                  ))}
              </DataTable>
              {!manifest.data.bookings.length && (
                <p className="text-sm text-muted-foreground">
                  No passengers on this manifest.
                </p>
              )}
            </>
          )}
        </Panel>
      )}
    </main>
  );
}
export function AssistancePage() {
  return <RequestQueue type="Assistance" title="Passenger assistance" description="Track accessibility and journey assistance from request to resolution." />;
}
