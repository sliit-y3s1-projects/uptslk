import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { apiClient } from "@/lib/api/api-client";
import { departureLabel } from "./ticket";

type Trip = {
  id: string;
  scheduledTime: string;
  status: string;
  route: string;
  routeName?: string;
  vehicle?: string;
  capacity: number;
  occupied: number;
  available: number;
  isFull: boolean;
};
type CheckoutSession = { url: string };
type PaymentStatus = {
  bookingId: string;
  status:
    | "Initiated"
    | "Pending"
    | "Succeeded"
    | "Failed"
    | "Cancelled"
    | "Chargebacked"
    | "RefundPending"
    | "Refunded";
};

export function CheckoutPage() {
  const [params] = useSearchParams();
  const routeId = params.get("routeId");
  const directionId = params.get("directionId");
  const date = params.get("date");
  const passengers = Number(params.get("passengers") ?? 1);
  const [tripId, setTripId] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);
  const trips = useQuery({
    queryKey: ["booking-trips", routeId, directionId, date],
    enabled: Boolean(routeId),
    queryFn: () =>
      apiClient<Trip[]>(
        `/api/v1/trips?bookableOnly=true&routeId=${routeId}${directionId ? `&directionId=${directionId}` : ""}${date ? `&date=${date}` : ""}`,
      ),
    refetchInterval: 30000,
  });
  const departures =
    trips.data?.filter(
      (trip) =>
        new Date(trip.scheduledTime).getTime() > now &&
        ["Scheduled", "Ready", "Boarding"].includes(trip.status),
    ) ?? [];
  const selectedTrip = departures.find(
    (trip) => trip.id === tripId && trip.available >= passengers,
  );
  async function confirm() {
    if (
      !selectedTrip ||
      new Date(selectedTrip.scheduledTime).getTime() <= now
    ) {
      setError(
        "Bookings have closed for this departure. Please choose a later trip.",
      );
      return;
    }
    setError("");
    setSubmitting(true);
    try {
      const session = await apiClient<CheckoutSession>(
        "/api/v1/payments/checkout/me",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ tripId, passengerCount: passengers }),
        },
      );
      window.location.assign(session.url);
    } catch (cause) {
      setError(
        cause instanceof Error
          ? cause.message
          : "Unable to start secure checkout",
      );
      setSubmitting(false);
    }
  }
  return (
    <main className="min-h-screen bg-slate-50 px-5 py-10">
      <section className="mx-auto max-w-3xl rounded-2xl border border-slate-200 bg-white p-8">
        <p className="text-sm font-medium text-primary">UPTSLK booking</p>
        <h1 className="mt-2 text-2xl font-semibold">Confirm your journey</h1>
        <p className="mt-2 text-sm text-slate-500">
          {passengers} passenger{passengers === 1 ? "" : "s"}. Choose a
          departure. Seats are not assigned for this urban service.
        </p>
        <>
          <div className="mt-7 space-y-3">
            <h2 className="font-semibold">Available departures</h2>
            {trips.isLoading && (
              <p className="text-sm text-slate-500">Loading departures...</p>
            )}
            {trips.error && (
              <p className="text-sm text-red-600">
                Unable to load departures. Please try again.
              </p>
            )}
            {!trips.isLoading && !trips.error && departures.length === 0 && (
              <p className="text-sm text-slate-500">
                No departures are available for this route.
              </p>
            )}
            {departures.map((trip) => (
              <button
                type="button"
                key={trip.id}
                onClick={() => {
                  setTripId(trip.id);
                }}
                disabled={trip.available < passengers}
                className={`w-full rounded-xl border p-4 text-left ${trip.available < passengers ? "cursor-not-allowed border-slate-200 bg-slate-100 text-slate-400" : tripId === trip.id ? "border-primary bg-primary/5" : "border-slate-200"}`}
              >
                <span className="font-medium">
                  {departureLabel(trip.scheduledTime)}
                </span>
                <span className="ml-3 text-sm text-slate-500">
                  {trip.status}
                </span>
                <span
                  className={`mt-2 block text-sm font-medium ${trip.available < passengers ? "text-rose-600" : "text-emerald-700"}`}
                >
                  {trip.isFull
                    ? "Full"
                    : trip.available < passengers
                      ? `Only ${trip.available} spaces available`
                      : `${trip.available} of ${trip.capacity} spaces available`}
                </span>
              </button>
            ))}
          </div>
          {selectedTrip && (
            <div className="mt-7 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">
              <p className="font-semibold">Boarding capacity confirmed</p>
              <p className="mt-1">
                This journey has {selectedTrip.available} spaces remaining. Your
                payment covers {passengers} passenger
                {passengers === 1 ? "" : "s"}; seats are not assigned.
              </p>
            </div>
          )}
          {error && <p className="mt-5 text-sm text-red-600">{error}</p>}
          <Button
            className="mt-7 w-full rounded-full bg-primary"
            disabled={!selectedTrip || submitting}
            onClick={confirm}
          >
            {submitting
              ? "Opening secure checkout..."
              : "Continue to secure payment"}
          </Button>
        </>
        <Button
          render={<Link to="/reservation" />}
          variant="link"
          className="mt-3 w-full"
        >
          Back to search
        </Button>
      </section>
    </main>
  );
}

export function BookingPaymentStatusPage({
  cancelled = false,
}: {
  cancelled?: boolean;
}) {
  const [params] = useSearchParams();
  const orderId = params.get("orderId") ?? "";
  const payment = useQuery({
    queryKey: ["public-payment-order", orderId],
    queryFn: () =>
      apiClient<PaymentStatus>(
        `/api/v1/payments/orders/${encodeURIComponent(orderId)}`,
      ),
    enabled: !!orderId,
    retry: false,
    refetchInterval: (query) =>
      query.state.data?.status === "Initiated" ||
      query.state.data?.status === "Pending"
        ? 2500
        : false,
  });
  const message = cancelled
    ? "Payment was cancelled. Your booking was not confirmed."
    : payment.data?.status === "Succeeded"
      ? "Payment confirmed. Your boarding pass is ready."
      : payment.data?.status === "Pending" ||
          payment.data?.status === "Initiated"
        ? "Waiting for payment confirmation. This page updates automatically."
        : "Payment was not completed. You can search for another departure.";
  return (
    <main className="min-h-screen bg-slate-50 px-5 py-10">
      <section className="mx-auto max-w-xl rounded-2xl border border-slate-200 bg-white p-8">
        <p className="text-sm font-medium text-primary">UPTSLK payment</p>
        <h1 className="mt-2 text-2xl font-semibold">Payment status</h1>
        <p className="mt-3 text-sm text-slate-600">{message}</p>
        {payment.error && (
          <p className="mt-4 text-sm text-red-600">
            Unable to retrieve payment status.
          </p>
        )}
        <Link to="/reservation">
          <Button className="mt-7 w-full rounded-full bg-primary">
            Return to journey search
          </Button>
        </Link>
        {payment.data?.status === "Succeeded" && (
          <Link to="/my-tickets">
            <Button variant="outline" className="mt-3 w-full">
              View my tickets
            </Button>
          </Link>
        )}
      </section>
    </main>
  );
}
