import { useState } from "react";
import {
  BusFront,
  CalendarDays,
  CheckCircle2,
  ChevronRight,
  CircleAlert,
  MapPin,
  QrCode,
  Ticket as TicketIcon,
} from "lucide-react";
import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { apiClient } from "@/lib/api/api-client";

type Ticket = {
  id: string;
  status: "Pending" | "Confirmed" | "Completed" | "Cancelled";
  passengerCount: number;
  fare: number;
  qrCode: string;
  createdAt: string;
  tripTime: string;
  route: string;
  routeName: string;
};

const statusStyle = {
  Confirmed: "bg-emerald-100 text-emerald-800",
  Pending: "bg-amber-100 text-amber-800",
  Completed: "bg-slate-100 text-slate-700",
  Cancelled: "bg-rose-50 text-rose-700",
};

export function MyTicketsPage() {
  const [selectedTicket, setSelectedTicket] = useState<Ticket | null>(null);
  const tickets = useQuery({
    queryKey: ["my-tickets"],
    queryFn: () => apiClient<Ticket[]>("/api/v1/bookings/me"),
  });
  const activeTickets =
    tickets.data?.filter(
      (ticket) => ticket.status === "Confirmed" || ticket.status === "Pending",
    ) ?? [];

  return (
    <main className="min-h-screen bg-slate-50">
      <section className="mx-auto max-w-3xl px-4 py-6 sm:px-6 sm:py-10">
        <div className="flex items-end justify-between gap-4">
          <h1 className="text-2xl font-bold tracking-tight text-slate-950 sm:text-3xl">
            My tickets
          </h1>
          <Link to="/">
            <Button className="hidden rounded-full sm:inline-flex">
              Book a journey
            </Button>
          </Link>
        </div>
        {tickets.isLoading && <TicketSkeleton />}
        {tickets.error && (
          <p className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
            Unable to load your tickets. Please refresh and try again.
          </p>
        )}
        {!tickets.isLoading && tickets.data?.length === 0 && (
          <div className="mt-8 rounded-3xl border border-dashed border-slate-300 bg-white p-8 text-center">
            <TicketIcon className="mx-auto size-9 text-slate-400" />
            <h2 className="mt-4 font-semibold">No tickets yet</h2>
            <p className="mt-1 text-sm text-slate-500">
              Your paid bookings will appear here.
            </p>
            <Link to="/">
              <Button className="mt-5 rounded-full">Find a journey</Button>
            </Link>
          </div>
        )}
        {activeTickets.length > 0 && (
          <TicketGroup label="Active tickets">
            {activeTickets.map((ticket) => (
              <TicketCard
                key={ticket.id}
                ticket={ticket}
                onView={() => setSelectedTicket(ticket)}
              />
            ))}
          </TicketGroup>
        )}
        {(tickets.data?.length ?? 0) > activeTickets.length && (
          <TicketGroup label="Past and cancelled">
            {tickets.data
              ?.filter((ticket) => !activeTickets.includes(ticket))
              .map((ticket) => (
                <TicketCard
                  key={ticket.id}
                  ticket={ticket}
                  onView={() => setSelectedTicket(ticket)}
                  muted
                />
              ))}
          </TicketGroup>
        )}
        <Link to="/" className="mt-6 block sm:hidden">
          <Button className="w-full rounded-full">Book a journey</Button>
        </Link>
      </section>
      <TicketDialog
        ticket={selectedTicket}
        onOpenChange={(open) => !open && setSelectedTicket(null)}
      />
    </main>
  );
}

function TicketGroup({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="mt-8">
      <p className="mb-3 text-sm font-semibold text-slate-600">{label}</p>
      <div className="space-y-3">{children}</div>
    </div>
  );
}

function TicketCard({
  ticket,
  onView,
  muted = false,
}: {
  ticket: Ticket;
  onView: () => void;
  muted?: boolean;
}) {
  const departure = new Date(ticket.tripTime);
  return (
    <article
      className={`overflow-hidden rounded-2xl border-2 bg-white shadow-sm ${muted ? "border-slate-300 opacity-80" : "border-slate-300"}`}
    >
      <div className="flex items-start gap-3 p-4 sm:p-5">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-indigo-50 text-indigo-700">
          <BusFront className="size-5" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="font-semibold text-slate-950">
              {ticket.route}{" "}
              <span className="font-normal text-slate-400">·</span>{" "}
              {ticket.routeName}
            </p>
            <span
              className={`rounded-full px-2.5 py-1 text-xs font-semibold ${statusStyle[ticket.status]}`}
            >
              {ticket.status === "Confirmed"
                ? "Approved to board"
                : ticket.status}
            </span>
          </div>
          <p className="mt-1 flex items-center gap-1.5 text-sm text-slate-600">
            <CalendarDays className="size-3.5" />
            {departure.toLocaleDateString([], {
              weekday: "short",
              month: "short",
              day: "numeric",
            })}{" "}
            ·{" "}
            {departure.toLocaleTimeString([], {
              hour: "numeric",
              minute: "2-digit",
            })}
          </p>
        </div>
      </div>
      <div className="grid grid-cols-3 border-y border-dashed border-slate-300 bg-slate-50/70 px-4 py-3 text-sm sm:px-5">
        <TicketFact label="Passengers" value={String(ticket.passengerCount)} />
        <TicketFact label="Fare" value={`LKR ${ticket.fare.toFixed(2)}`} />
        <TicketFact
          label="Boarding"
          value={ticket.status === "Confirmed" ? "Ready" : ticket.status}
        />
      </div>
      <div className="flex items-center justify-between px-4 py-3 sm:px-5">
        <p className="text-xs text-slate-500">
          {ticket.status === "Confirmed"
            ? "Show this pass when boarding."
            : "Ticket details and payment state."}
        </p>
        <Button
          variant="ghost"
          size="sm"
          className="gap-1 rounded-full text-primary"
          onClick={onView}
        >
          View ticket <ChevronRight className="size-4" />
        </Button>
      </div>
    </article>
  );
}

function TicketFact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-slate-500">{label}</p>
      <p className="mt-0.5 truncate font-semibold">{value}</p>
    </div>
  );
}

function TicketDialog({
  ticket,
  onOpenChange,
}: {
  ticket: Ticket | null;
  onOpenChange: (open: boolean) => void;
}) {
  return (
    <Dialog open={Boolean(ticket)} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto rounded-3xl p-5 sm:p-6">
        <DialogHeader>
          <DialogTitle>Your boarding pass</DialogTitle>
          <DialogDescription>
            {ticket?.status === "Confirmed"
              ? "Approved for boarding. Show this code to the conductor."
              : "This ticket is not currently approved for boarding."}
          </DialogDescription>
        </DialogHeader>
        {ticket && (
          <div className="space-y-5">
            <div className="rounded-2xl bg-indigo-950 p-5 text-white">
              <div>
                <p className="text-sm text-indigo-200">{ticket.route}</p>
                <p className="mt-1 text-xl font-semibold">{ticket.routeName}</p>
              </div>
              <p className="mt-5 flex items-center gap-2 text-sm text-indigo-100">
                <MapPin className="size-4" />
                {new Date(ticket.tripTime).toLocaleString()}
              </p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-white p-5 text-center">
              <div className="mx-auto grid w-48 grid-cols-[repeat(21,minmax(0,1fr))] gap-px rounded bg-white p-2">
                <QrPattern value={ticket.qrCode} />
              </div>
              <div className="mt-4 flex items-center justify-center gap-2 text-sm font-semibold text-slate-800">
                <QrCode className="size-4 text-primary" />
                Boarding code
              </div>
              <p className="mt-1 break-all font-mono text-xs text-slate-500">
                {ticket.qrCode}
              </p>
            </div>
            <div className="flex items-start gap-3 rounded-xl bg-emerald-50 p-3 text-sm text-emerald-800">
              {ticket.status === "Confirmed" ? (
                <CheckCircle2 className="mt-0.5 size-4 shrink-0" />
              ) : (
                <CircleAlert className="mt-0.5 size-4 shrink-0" />
              )}
              <p>
                {ticket.status === "Confirmed"
                  ? "Payment is confirmed and this ticket is ready for boarding."
                  : "This code is a sample boarding pass. It becomes usable when payment is confirmed."}
              </p>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

function QrPattern({ value }: { value: string }) {
  const cells = Array.from({ length: 441 }, (_, index) => {
    const row = Math.floor(index / 21);
    const column = index % 21;
    const finder =
      (row < 7 && column < 7) ||
      (row < 7 && column > 13) ||
      (row > 13 && column < 7);
    const finderFill =
      finder &&
      (row % 6 === 0 ||
        column % 6 === 0 ||
        (row % 6 >= 2 && row % 6 <= 4 && column % 6 >= 2 && column % 6 <= 4));
    const valueCode = value.charCodeAt(index % value.length) || 0;
    return (
      <span
        key={index}
        className={`aspect-square ${finder ? (finderFill ? "bg-slate-950" : "bg-white") : (valueCode * (index + 17) + row * 13 + column * 7) % 5 < 2 ? "bg-slate-950" : "bg-white"}`}
      />
    );
  });
  return <>{cells}</>;
}

function TicketSkeleton() {
  return (
    <div className="mt-8 space-y-3">
      {[1, 2].map((item) => (
        <div
          key={item}
          className="h-44 animate-pulse rounded-2xl border border-slate-200 bg-white"
        />
      ))}
    </div>
  );
}
