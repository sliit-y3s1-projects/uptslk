import { useState } from "react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import {
  Panel,
  Field,
  Feedback,
  QueryState,
  DataTable,
} from "@/features/riders/components/FeatureUi";
import { money, dateTime } from "@/features/riders/components/format";
import {
  useBooking,
  useBookingMutations,
  useTrip,
} from "../hooks/useBookings";
import { TicketQr } from "./TicketQr";
export function BookingTicket({ id }: { id: string }) {
  const query = useBooking(id);
  const booking = query.data;
  const trip = useTrip(booking?.trip.id ?? "");
  const { cancel, complete } = useBookingMutations();
  const [reason, setReason] = useState("");
  const [action, setAction] = useState<"cancel" | "complete" | "">("");
  const [notice, setNotice] = useState("");
  const pending = cancel.isPending || complete.isPending;
  const canComplete =
    booking?.status === "Confirmed" &&
    !!trip.data &&
    ["Dispatched", "Completed"].includes(trip.data.status);
  const canCancel =
    booking && ["Pending", "Confirmed"].includes(booking.status);
  const done = (message: string) => {
    setAction("");
    setNotice(message);
    setReason("");
  };
  return (
    <div className="space-y-4">
      <QueryState query={query} />
      {booking && (
        <Panel title="Digital ticket">
          <div className="grid gap-6 sm:grid-cols-[1fr_180px]">
            <div className="space-y-4">
              <p className="break-all font-mono text-xs text-muted-foreground">
                Booking {booking.id}
              </p>
              <h3 className="text-lg font-semibold">
                {booking.trip.route} · {booking.trip.name}
              </h3>
              <dl className="grid grid-cols-2 gap-4 text-sm">
                {[
                  ["Passenger", booking.passenger.fullName],
                  ["Departure", dateTime(booking.trip.scheduledTime)],
                  ["Vehicle", booking.trip.vehicle],
                  ["Bay", trip.data?.bay.code ?? "Unavailable"],
                  ["Passengers", String(booking.passengerCount)],
                  ["Fare", money(booking.fare)],
                  ["Category", booking.passengerCategory],
                  ["Status", booking.status],
                ].map(([label, value]) => (
                  <div key={label}>
                    <dt className="text-muted-foreground">{label}</dt>
                    <dd className="mt-1 font-medium">{value}</dd>
                  </div>
                ))}
              </dl>
              <Link
                className="text-sm underline"
                to={`/riders/accounts?passengerId=${booking.passenger.id}`}
              >
                Open passenger and wallet
              </Link>
            </div>
            <div className="space-y-2">
              <TicketQr value={booking.qrCode} />
              <p className="break-all font-mono text-xs">{booking.qrCode}</p>
            </div>
          </div>
          <QueryState query={trip} />
          <Feedback
            error={cancel.error || complete.error}
            success={notice}
          />
          {booking.status === "Cancelled" && (
            <div className="space-y-1 rounded-md border p-4 text-sm">
              <p className="font-medium">
                Refund recorded: {money(booking.refundAmount)}
              </p>
              <p>Reason: {booking.cancellationReason}</p>
              <p>
                Wallet balance:{" "}
                {booking.passenger.balance === null
                  ? "Unavailable"
                  : money(booking.passenger.balance)}
              </p>
            </div>
          )}
          <div className="flex flex-wrap gap-2">
            {canCancel && (
              <Button
                variant="outline"
                disabled={pending}
                onClick={() => {
                  setAction("cancel");
                  cancel.reset();
                }}
              >
                Cancel and refund
              </Button>
            )}
            {canComplete && (
              <Button
                variant="outline"
                disabled={pending}
                onClick={() => {
                  setAction("complete");
                  complete.reset();
                }}
              >
                Complete booking
              </Button>
            )}
            <Link
              className="self-center text-sm underline"
              to={`/passengers/flow?tripId=${booking.trip.id}`}
            >
              Passenger manifest
            </Link>
          </div>
          {action === "cancel" && canCancel && (
            <form
              className="space-y-3"
              onSubmit={(e) => {
                e.preventDefault();
                if (reason.trim())
                  cancel.mutate(
                    { id, reason: reason.trim() },
                    {
                      onSuccess: () =>
                        done(
                          "Booking cancelled. Refund and wallet balance refreshed.",
                        ),
                    },
                  );
              }}
            >
              <p className="text-sm">
                Cancel this booking and refund {money(booking.fare)} to the
                passenger wallet.
              </p>
              <Field
                label="Cancellation reason"
                required
                maxLength={1000}
                value={reason}
                disabled={pending}
                onChange={(e) => setReason(e.target.value)}
              />
              <Button type="submit" disabled={pending || !reason.trim()}>
                {pending ? "Cancelling..." : "Confirm cancellation and refund"}
              </Button>
            </form>
          )}
          {action === "complete" && canComplete && (
            <div className="space-y-3">
              <p className="text-sm">
                Mark this journey as completed? Completed bookings cannot be
                cancelled or refunded through this action.
              </p>
              <Button
                disabled={pending}
                onClick={() =>
                  complete.mutate(id, {
                    onSuccess: () => done("Booking completed."),
                  })
                }
              >
                Confirm completion
              </Button>
            </div>
          )}
          {action && (
            <Button
              variant="ghost"
              disabled={pending}
              onClick={() => setAction("")}
            >
              Dismiss
            </Button>
          )}
          <h3 className="text-sm font-medium">Ticket transactions</h3>
          <DataTable headings={["Date", "Type", "Amount"]}>
            {booking.transactions.map((t) => (
              <tr key={t.id}>
                <td>{dateTime(t.createdAt)}</td>
                <td>{t.type}</td>
                <td>{money(t.amount)}</td>
              </tr>
            ))}
          </DataTable>
        </Panel>
      )}
    </div>
  );
}
