import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { useSearchParams } from "react-router";
import { SlidersHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { PageHeading } from "@/components/shared/PageHeading";
import { useAuth } from "@/hooks/useAuth";
import { PassengerWorkspace } from "@/features/riders/RiderPages";
import {
  Panel,
  Field,
  QueryState,
  Feedback,
} from "@/features/riders/components/FeatureUi";
import { money, dateTime } from "@/features/riders/components/format";
import { usePassengers } from "@/features/riders/hooks/usePassengers";
import { useBookings, useTrips } from "./hooks/useBookings";
import { bookingsService } from "./services/bookings.service";
import { CreateBooking } from "./components/CreateBooking";
import { BookingTicket } from "./components/BookingTicket";
import { FareRules } from "./components/FareRules";

const BOOKING_PAGE_SIZE = 50;

function BookingFilterSelect({
  label,
  value,
  onValueChange,
  items,
}: {
  label: string;
  value: string;
  onValueChange: (value: string) => void;
  items: { value: string; label: string }[];
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-sm font-medium">{label}</label>
      <Select
        value={value}
        onValueChange={(nextValue) => onValueChange(String(nextValue ?? "all"))}
      >
        <SelectTrigger className="h-11 w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {items.map((item) => (
            <SelectItem key={item.value} value={item.value}>
              {item.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}

function BookingStatus({ status }: { status: string }) {
  const style =
    status === "Confirmed"
      ? "bg-emerald-100 text-emerald-800"
      : status === "Pending"
        ? "bg-amber-100 text-amber-800"
        : status === "Cancelled"
          ? "bg-rose-100 text-rose-800"
          : "bg-slate-100 text-slate-700";
  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${style}`}>
      {status}
    </span>
  );
}

/** Kept for existing deep links. New navigation uses /fares/bookings. */
export function TicketsPage() {
  return <BookingManagementPage />;
}

export function BookingManagementPage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const id = params.get("bookingId") ?? "";
  const [adding, setAdding] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState({
    passengerId: "all",
    tripId: "all",
    status: "all",
  });
  const [draftFilters, setDraftFilters] = useState(filters);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const centreId = user?.centreId ?? "";
  const trips = useTrips(centreId);
  const passengers = usePassengers();
  const query = useBookings({
    passengerId: filters.passengerId === "all" ? "" : filters.passengerId,
    tripId: filters.tripId === "all" ? "" : filters.tripId,
    status: filters.status === "all" ? "" : filters.status,
  });
  const bookings = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (query.data ?? []).filter(
      (booking) =>
        !term ||
        [booking.passenger, booking.route].some((value) =>
          value.toLowerCase().includes(term),
        ),
    );
  }, [query.data, search]);
  const pageCount = Math.max(1, Math.ceil(bookings.length / BOOKING_PAGE_SIZE));
  const visibleBookings = bookings.slice(
    page * BOOKING_PAGE_SIZE,
    (page + 1) * BOOKING_PAGE_SIZE,
  );
  const appliedFilterCount = Object.values(filters).filter(
    (value) => value !== "all",
  ).length;

  if (id) {
    return (
      <main className="flex flex-1 flex-col bg-muted/20 p-4 lg:p-6">
        <div className="mx-auto flex w-full max-w-[1200px] flex-col gap-5">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <PageHeading
              title="Booking ticket"
              description="Review the ticket, payment history and any permitted booking actions."
            />
            <Button variant="outline" onClick={() => setParams({})}>
              Back to bookings
            </Button>
          </div>
          <BookingTicket key={id} id={id} />
        </div>
      </main>
    );
  }

  return (
    <main className="flex flex-1 flex-col bg-muted/20 p-4 lg:p-6">
      <div className="mx-auto flex w-full max-w-[1600px] flex-col gap-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <PageHeading
            title="Bookings"
            description="Track passenger journeys, open tickets and begin a new secure booking."
          />
          <div className="flex flex-wrap gap-2">
            <Button
              variant="outline"
              className="gap-2"
              onClick={() => {
                setDraftFilters(filters);
                setFiltersOpen(true);
              }}
            >
              <SlidersHorizontal className="size-4" />
              Filters
              {appliedFilterCount > 0 && (
                <span className="rounded-full bg-primary px-1.5 text-xs text-primary-foreground">
                  {appliedFilterCount}
                </span>
              )}
            </Button>
            <Button onClick={() => setAdding(true)}>Create booking</Button>
          </div>
        </div>
        <Panel title="Booking register">
          <div className="flex flex-col gap-4 border-b pb-4 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-xl">
              <p className="font-medium">Current centre bookings</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Search the register first, then open the individual ticket only
                when you need to act.
              </p>
            </div>
            <div className="w-full lg:max-w-md">
              <Field
                label="Quick search"
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(0);
                }}
                placeholder="Passenger, route or seat number"
              />
            </div>
          </div>
          {!centreId && (
            <p className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
              Your account is not assigned to a centre, so bookings cannot be
              loaded.
            </p>
          )}
          <QueryState query={passengers} />
          {centreId && <QueryState query={trips} />}
          <QueryState query={query} empty={!query.data?.length} />
          {query.data && !query.error && (
            <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
              <p>
                {bookings.length.toLocaleString()} matching booking
                {bookings.length === 1 ? "" : "s"}
              </p>
              <p>
                Showing{" "}
                {bookings.length
                  ? `${page * BOOKING_PAGE_SIZE + 1}–${Math.min((page + 1) * BOOKING_PAGE_SIZE, bookings.length)}`
                  : "0"}
              </p>
            </div>
          )}
          {query.data && !query.error && (
            <div className="overflow-hidden rounded-xl border">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[820px] text-left text-sm">
                  <thead className="bg-muted/60 text-muted-foreground">
                    <tr>
                      <th className="px-5 py-3 font-medium">Passenger</th>
                      <th className="px-5 py-3 font-medium">Journey</th>
                      <th className="px-5 py-3 font-medium">Passengers</th>
                      <th className="px-5 py-3 font-medium">Fare</th>
                      <th className="px-5 py-3 font-medium">Status</th>
                      <th className="px-5 py-3 text-right font-medium">
                        Ticket
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {visibleBookings.map((booking) => (
                      <tr
                        key={booking.id}
                        className="bg-card transition-colors hover:bg-muted/30"
                      >
                        <td className="px-5 py-4">
                          <p className="font-semibold">{booking.passenger}</p>
                        </td>
                        <td className="px-5 py-4">
                          <p className="font-medium">{booking.route}</p>
                          <p className="mt-0.5 text-muted-foreground">
                            {dateTime(booking.tripTime)}
                          </p>
                        </td>
                        <td className="px-5 py-4 font-semibold">
                          {booking.passengerCount}
                        </td>
                        <td className="px-5 py-4 font-semibold">
                          {money(booking.fare)}
                        </td>
                        <td className="px-5 py-4">
                          <BookingStatus status={booking.status} />
                        </td>
                        <td className="px-5 py-4 text-right">
                          <Button
                            size="sm"
                            className="bg-primary/10 text-primary hover:bg-primary/20"
                            variant="ghost"
                            onClick={() => setParams({ bookingId: booking.id })}
                          >
                            Open ticket
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
          {bookings.length > BOOKING_PAGE_SIZE && (
            <div className="flex items-center justify-end gap-3">
              <span className="text-sm text-muted-foreground">
                Page {page + 1} of {pageCount}
              </span>
              <Button
                size="sm"
                variant="outline"
                disabled={page === 0}
                onClick={() => setPage((current) => current - 1)}
              >
                Previous
              </Button>
              <Button
                size="sm"
                variant="outline"
                disabled={page >= pageCount - 1}
                onClick={() => setPage((current) => current + 1)}
              >
                Next
              </Button>
            </div>
          )}
        </Panel>
        <Sheet open={filtersOpen} onOpenChange={setFiltersOpen}>
          <SheetContent side="right" className="sm:!max-w-sm">
            <SheetHeader>
              <SheetTitle>Filter bookings</SheetTitle>
              <SheetDescription>
                Choose filters for the booking register, then apply them
                together.
              </SheetDescription>
            </SheetHeader>
            <div className="space-y-5 py-2">
              <BookingFilterSelect
                label="Passenger"
                value={draftFilters.passengerId}
                onValueChange={(value) =>
                  setDraftFilters((current) => ({
                    ...current,
                    passengerId: value,
                  }))
                }
                items={[
                  { value: "all", label: "All passengers" },
                  ...(passengers.data ?? []).map((passenger) => ({
                    value: passenger.id,
                    label: `${passenger.fullName} · ${passenger.phoneNumber}`,
                  })),
                ]}
              />
              <BookingFilterSelect
                label="Scheduled trip"
                value={draftFilters.tripId}
                onValueChange={(value) =>
                  setDraftFilters((current) => ({ ...current, tripId: value }))
                }
                items={[
                  { value: "all", label: "All trips" },
                  ...(trips.data ?? []).map((trip) => ({
                    value: trip.id,
                    label: `${trip.routeNumber} · ${dateTime(trip.scheduledTime)}`,
                  })),
                ]}
              />
              <BookingFilterSelect
                label="Booking status"
                value={draftFilters.status}
                onValueChange={(value) =>
                  setDraftFilters((current) => ({ ...current, status: value }))
                }
                items={[
                  { value: "all", label: "All statuses" },
                  ...["Pending", "Confirmed", "Completed", "Cancelled"].map(
                    (status) => ({ value: status, label: status }),
                  ),
                ]}
              />
            </div>
            <SheetFooter>
              <Button
                variant="outline"
                onClick={() =>
                  setDraftFilters({
                    passengerId: "all",
                    tripId: "all",
                    status: "all",
                  })
                }
              >
                Clear
              </Button>
              <Button
                onClick={() => {
                  setFilters(draftFilters);
                  setPage(0);
                  setFiltersOpen(false);
                }}
              >
                Apply filters
              </Button>
            </SheetFooter>
          </SheetContent>
        </Sheet>
        <Dialog open={adding} onOpenChange={setAdding}>
          <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-3xl">
            <DialogHeader>
              <DialogTitle>Create booking</DialogTitle>
              <DialogDescription>
                Select an eligible journey, passenger and available seat. The
                customer completes payment through the secure checkout.
              </DialogDescription>
            </DialogHeader>
            <CreateBooking />
          </DialogContent>
        </Dialog>
      </div>
    </main>
  );
}

export function FareRulesManagementPage() {
  return (
    <main className="flex flex-1 flex-col bg-muted/20 p-4 lg:p-6">
      <div className="mx-auto flex w-full max-w-[1600px] flex-col gap-5">
        <PageHeading
          title="Fare rules"
          description="Set and maintain route fares by passenger category. Changes apply only to new bookings."
        />
        <FareRules />
      </div>
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

export function PaymentReturnPage({
  cancelled = false,
}: {
  cancelled?: boolean;
}) {
  const [params] = useSearchParams();
  const orderId = params.get("orderId") ?? "";
  const payment = useQuery({
    queryKey: ["payment-order", orderId],
    queryFn: () => bookingsService.paymentOrder(orderId),
    enabled: !!orderId,
    retry: false,
    refetchInterval: (query) =>
      query.state.data?.status === "Pending" ||
      query.state.data?.status === "Initiated"
        ? 2500
        : false,
  });
  const message = cancelled
    ? "Payment was cancelled. The booking has not been confirmed."
    : payment.data?.status === "Succeeded"
      ? "Payment confirmed. Your ticket is ready."
      : payment.data?.status === "Pending" ||
          payment.data?.status === "Initiated"
        ? "We are waiting for PayHere to confirm the payment. This page updates automatically."
        : "Payment was not completed. You can return and create a new booking.";
  return (
    <main className="flex flex-1 flex-col gap-4 bg-muted/20 p-4">
      <PageHeading title="Payment status" description={message} />
      <Feedback error={payment.error} />
      {payment.data?.bookingId && payment.data.status === "Succeeded" && (
        <Link to={`/fares/tickets?bookingId=${payment.data.bookingId}`}>
          <Button>View ticket</Button>
        </Link>
      )}
      <Link to="/fares/tickets">
        <Button variant="outline">Return to bookings</Button>
      </Link>
    </main>
  );
}
