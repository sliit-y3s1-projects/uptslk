import { useEffect, useState } from "react";
import { Download, Ticket as TicketIcon } from "lucide-react";
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
import { TicketQr } from "@/features/fares/components/TicketQr";
import {
  canBoard,
  departureLabel,
  ticketGroup,
  ticketLabel,
  type Ticket,
} from "./ticket";

const groups = ["Upcoming", "Past", "Cancelled"] as const;

export function MyTicketsPage() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [group, setGroup] = useState<(typeof groups)[number]>("Upcoming");
  const [now, setNow] = useState(Date.now);
  const [downloading, setDownloading] = useState(false);
  const [downloadError, setDownloadError] = useState("");
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);
  const tickets = useQuery({
    queryKey: ["my-tickets"],
    queryFn: () => apiClient<Ticket[]>("/api/v1/bookings/me"),
    refetchInterval: 30000,
  });
  const allTickets = tickets.data ?? [];
  const visible = allTickets
    .filter((ticket) => ticketGroup(ticket, now) === group)
    .sort((a, b) =>
      group === "Upcoming"
        ? Date.parse(a.tripTime) - Date.parse(b.tripTime)
        : Date.parse(b.tripTime) - Date.parse(a.tripTime),
    );
  const selected = allTickets.find((ticket) => ticket.id === selectedId);

  async function download() {
    if (!selectedId) return;
    setDownloading(true);
    setDownloadError("");
    try {
      const result = await tickets.refetch();
      if (result.error) throw result.error;
      const current = result.data?.find((ticket) => ticket.id === selectedId);
      if (!current || !canBoard(current))
        throw new Error("This booking no longer has a valid boarding pass.");
      const { downloadTicketPdf } = await import("./ticketPdf");
      await downloadTicketPdf(current);
    } catch (error) {
      setDownloadError(
        error instanceof Error
          ? error.message
          : "Could not download your ticket. Please try again.",
      );
    } finally {
      setDownloading(false);
    }
  }

  return (
    <main className="min-h-screen bg-slate-50">
      <section className="mx-auto max-w-3xl px-4 py-6 sm:px-6 sm:py-10">
        <div className="flex items-center justify-between gap-4">
          <h1 className="text-2xl font-semibold tracking-tight">My tickets</h1>
          <Button variant="outline" render={<Link to="/reservation" />}>
            Book a journey
          </Button>
        </div>
        <div className="my-6 flex flex-wrap gap-2" aria-label="Filter bookings">
          {groups.map((item) => (
            <Button
              key={item}
              variant={group === item ? "default" : "outline"}
              aria-pressed={group === item}
              onClick={() => setGroup(item)}
            >
              {item}{" "}
              <span className="opacity-70">
                {
                  allTickets.filter(
                    (ticket) => ticketGroup(ticket, now) === item,
                  ).length
                }
              </span>
            </Button>
          ))}
        </div>
        {tickets.isLoading ? (
          <p role="status">Loading tickets...</p>
        ) : tickets.error ? (
          <p role="alert" className="text-destructive">
            Unable to load your tickets. Please try again.
          </p>
        ) : visible.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center">
            <TicketIcon className="mx-auto mb-3 size-7 text-muted-foreground" />
            <p>No {group.toLowerCase()} bookings.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {visible.map((ticket) => (
              <article
                key={ticket.id}
                className="rounded-xl border border-slate-300 bg-white p-5"
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-sm text-primary">{ticket.route}</p>
                    <h2 className="mt-1 font-semibold">
                      {ticket.origin} to {ticket.destination}
                    </h2>
                    <p className="mt-2 text-sm text-muted-foreground">
                      {departureLabel(ticket.tripTime)}
                    </p>
                  </div>
                  <span
                    className={`rounded-md px-2 py-1 text-xs ${canBoard(ticket, now) ? "bg-emerald-50 text-emerald-800" : "bg-slate-100 text-slate-600"}`}
                  >
                    {ticketLabel(ticket)}
                  </span>
                </div>
                <div className="mt-4 flex items-center justify-between gap-3 border-t pt-3">
                  <p className="text-sm text-muted-foreground">
                    {ticket.passengerCount} passenger
                    {ticket.passengerCount === 1 ? "" : "s"} · LKR{" "}
                    {ticket.fare.toFixed(2)}
                  </p>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setSelectedId(ticket.id);
                      setDownloadError("");
                      void tickets.refetch();
                    }}
                  >
                    {canBoard(ticket, now) ? "View ticket" : "View details"}
                  </Button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
      <Dialog
        open={!!selected}
        onOpenChange={(open) => !open && setSelectedId(null)}
      >
        <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              {selected && canBoard(selected, now)
                ? "Boarding ticket"
                : "Booking details"}
            </DialogTitle>
            <DialogDescription>
              {selected && canBoard(selected, now)
                ? "Show this QR when boarding. Times are in Sri Lanka time."
                : "This booking is not valid for boarding."}
            </DialogDescription>
          </DialogHeader>
          {selected && (
            <>
              <div className="rounded-lg border border-primary/20 bg-primary/5 p-4">
                <p className="text-sm text-primary">
                  {selected.route} · {ticketLabel(selected)}
                </p>
                <p className="mt-1 font-semibold">
                  {selected.origin} to {selected.destination}
                </p>
                <p className="mt-2 text-sm">
                  {departureLabel(selected.tripTime)}
                </p>
              </div>
              <dl className="grid grid-cols-2 gap-3 text-sm">
                {[
                  ["Passenger", selected.passengerName],
                  ["Departure bay", selected.bay],
                  ["Passengers", String(selected.passengerCount)],
                  ["Fare", `LKR ${selected.fare.toFixed(2)}`],
                ].map(([label, value]) => (
                  <div key={label}>
                    <dt className="text-muted-foreground">{label}</dt>
                    <dd className="mt-1 font-medium">{value}</dd>
                  </div>
                ))}
              </dl>
              {canBoard(selected, now) && selected.qrCode && (
                <>
                  <div className="flex flex-col items-center rounded-lg border bg-white p-5">
                    <TicketQr value={selected.qrCode} />
                    <p className="mt-3 break-all font-mono text-xs">
                      {selected.qrCode}
                    </p>
                  </div>
                  <Button
                    onClick={() => void download()}
                    disabled={downloading}
                  >
                    <Download />
                    {downloading ? "Preparing PDF..." : "Download ticket PDF"}
                  </Button>
                </>
              )}
              <p className="break-all text-xs text-muted-foreground">
                Booking reference: {selected.id}
              </p>
              {downloadError && (
                <p role="alert" className="text-sm text-destructive">
                  {downloadError}
                </p>
              )}
            </>
          )}
        </DialogContent>
      </Dialog>
    </main>
  );
}
