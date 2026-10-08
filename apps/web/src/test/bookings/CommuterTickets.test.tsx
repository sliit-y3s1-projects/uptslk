import { describe, expect, it } from "vitest";
import App from "@/App";
import { BookingPaymentStatusPage } from "@/features/bookings/CheckoutPage";
import { MyTicketsPage } from "@/features/bookings/MyTicketsPage";
import { canBoard, ticketGroup, ticketLabel, type Ticket } from "@/features/bookings/ticket";
import { buildStaff, buildUser } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";

const HOUR = 60 * 60 * 1000;
const NOW = Date.parse("2030-06-10T10:00:00Z");

function buildTicket(overrides: Partial<Ticket> = {}): Ticket {
  return {
    id: "ticket-1",
    status: "Confirmed",
    tripStatus: "Scheduled",
    passengerCount: 1,
    fare: 150,
    qrCode: "BKG-TEST-1",
    canBoard: true,
    tripTime: new Date(NOW + 24 * HOUR).toISOString(),
    route: "101",
    routeName: "Colombo Fort - Kandy",
    passengerName: "Nimal Perera",
    origin: "Colombo",
    destination: "Kandy",
    bay: "B1",
    ...overrides,
  };
}

describe("ticketGroup (pure function)", () => {
  it("puts a cancelled booking in Cancelled", () => {
    expect(ticketGroup(buildTicket({ status: "Cancelled" }), NOW)).toBe("Cancelled");
  });

  it("puts a ticket for a cancelled trip in Cancelled", () => {
    expect(ticketGroup(buildTicket({ tripStatus: "Cancelled" }), NOW)).toBe("Cancelled");
  });

  it("puts a completed booking or completed trip in Past", () => {
    expect(ticketGroup(buildTicket({ status: "Completed" }), NOW)).toBe("Past");
    expect(ticketGroup(buildTicket({ tripStatus: "Completed" }), NOW)).toBe("Past");
  });

  it("puts a future departure in Upcoming and a past departure in Past", () => {
    expect(ticketGroup(buildTicket({ tripTime: new Date(NOW + HOUR).toISOString() }), NOW)).toBe("Upcoming");
    expect(ticketGroup(buildTicket({ tripTime: new Date(NOW - 5 * HOUR).toISOString() }), NOW)).toBe("Past");
  });

  it("keeps a trip that is boarding, delayed or dispatched Upcoming until its active window ends", () => {
    const departed = new Date(NOW - HOUR).toISOString();
    for (const tripStatus of ["Boarding", "Delayed", "Dispatched"]) {
      const withinWindow = buildTicket({ tripStatus, tripTime: departed, activeUntil: new Date(NOW + HOUR).toISOString() });
      const afterWindow = buildTicket({ tripStatus, tripTime: departed, activeUntil: new Date(NOW - 1000).toISOString() });
      expect(ticketGroup(withinWindow, NOW)).toBe("Upcoming");
      expect(ticketGroup(afterWindow, NOW)).toBe("Past");
    }
  });

  it("falls back to a two hour window when the server sends no activeUntil", () => {
    const departed = (hoursAgo: number) => new Date(NOW - hoursAgo * HOUR).toISOString();

    expect(ticketGroup(buildTicket({ tripStatus: "Dispatched", tripTime: departed(1) }), NOW)).toBe("Upcoming");
    expect(ticketGroup(buildTicket({ tripStatus: "Dispatched", tripTime: departed(3) }), NOW)).toBe("Past");
  });
});

describe("canBoard and ticketLabel (pure functions)", () => {
  it("allows boarding only for a confirmed, upcoming ticket with a QR code that the server allows", () => {
    expect(canBoard(buildTicket(), NOW)).toBe(true);
    expect(canBoard(buildTicket({ status: "Pending" }), NOW)).toBe(false);
    expect(canBoard(buildTicket({ qrCode: null }), NOW)).toBe(false);
    expect(canBoard(buildTicket({ canBoard: false }), NOW)).toBe(false);
    expect(canBoard(buildTicket({ tripStatus: "Dispatched" }), NOW)).toBe(false);
    expect(canBoard(buildTicket({ status: "Cancelled" }), NOW)).toBe(false);
  });

  it("labels tickets by their state", () => {
    expect(ticketLabel(buildTicket({ status: "Cancelled" }))).toBe("Cancelled");
    expect(ticketLabel(buildTicket({ status: "Pending" }))).toBe("Awaiting payment");
    expect(ticketLabel(buildTicket({ status: "Completed", tripStatus: "Completed" }))).toBe("Completed");
    expect(ticketLabel(buildTicket({ tripTime: "2020-01-01T00:00:00Z" }))).toBe("Expired");
  });
});

describe("MyTicketsPage", () => {
  const upcoming = buildTicket({ id: "t-up", tripTime: new Date(Date.now() + 24 * HOUR).toISOString(), qrCode: "BKG-UPCOMING" });
  const past = buildTicket({
    id: "t-past", status: "Completed", tripStatus: "Completed", route: "202", origin: "Galle", destination: "Matara",
    tripTime: new Date(Date.now() - 48 * HOUR).toISOString(), qrCode: null, canBoard: false,
  });
  const cancelled = buildTicket({ id: "t-cancelled", status: "Cancelled", route: "303", origin: "Jaffna", destination: "Vavuniya" });

  const mockTickets = (tickets: Ticket[]) =>
    server.use(http.get(apiUrl("/api/v1/bookings/me"), () => HttpResponse.json(tickets)));

  it("requests the signed-in commuter's tickets and shows the Upcoming group first with counts on the filters", async () => {
    mockTickets([upcoming, past, cancelled]);

    renderWithProviders(<MyTicketsPage />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByText("Colombo to Kandy")).toBeInTheDocument();
    expect(screen.queryByText("Galle to Matara")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Upcoming/ })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /Upcoming/ })).toHaveTextContent("1");
    expect(screen.getByRole("button", { name: /Past/ })).toHaveTextContent("1");
    expect(screen.getByRole("button", { name: /Cancelled/ })).toHaveTextContent("1");
  });

  it("switches between Upcoming, Past and Cancelled", async () => {
    mockTickets([upcoming, past, cancelled]);
    const { user } = renderWithProviders(<MyTicketsPage />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });
    await screen.findByText("Colombo to Kandy");

    await user.click(screen.getByRole("button", { name: /Past/ }));
    expect(await screen.findByText("Galle to Matara")).toBeInTheDocument();
    expect(screen.queryByText("Colombo to Kandy")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /Cancelled/ }));
    expect(await screen.findByText("Jaffna to Vavuniya")).toBeInTheDocument();
  });

  it("shows an empty message for a group that has no bookings", async () => {
    mockTickets([upcoming]);
    const { user } = renderWithProviders(<MyTicketsPage />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });
    await screen.findByText("Colombo to Kandy");

    await user.click(screen.getByRole("button", { name: /Past/ }));

    expect(await screen.findByText("No past bookings.")).toBeInTheDocument();
  });

  it("shows a loading state and then an error message when the API fails", async () => {
    server.use(http.get(apiUrl("/api/v1/bookings/me"), () => HttpResponse.json({ error: "boom" }, { status: 500 })));

    renderWithProviders(<MyTicketsPage />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });

    expect(screen.getByRole("status")).toHaveTextContent("Loading tickets...");
    expect(await screen.findByRole("alert")).toHaveTextContent("Unable to load your tickets");
  });

  it("opens a boarding ticket with the QR code for a valid upcoming ticket", async () => {
    mockTickets([upcoming]);
    const { user } = renderWithProviders(<MyTicketsPage />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });
    await screen.findByText("Colombo to Kandy");

    await user.click(screen.getByRole("button", { name: "View ticket" }));

    expect(await screen.findByText("Boarding ticket")).toBeInTheDocument();
    expect(screen.getByText("BKG-UPCOMING")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Download ticket PDF/ })).toBeInTheDocument();
  });

  it("shows booking details without a QR code for a ticket that cannot be used to board", async () => {
    mockTickets([past]);
    const { user } = renderWithProviders(<MyTicketsPage />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });
    await user.click(await screen.findByRole("button", { name: /Past/ }));
    await screen.findByText("Galle to Matara");

    await user.click(screen.getByRole("button", { name: "View details" }));

    expect(await screen.findByText("This booking is not valid for boarding.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Download ticket PDF/ })).not.toBeInTheDocument();
  });
});

describe("BookingPaymentStatusPage (payment return)", () => {
  const mockOrder = (status: string) =>
    server.use(
      http.get(apiUrl("/api/v1/payments/orders/UPTS-1"), () => HttpResponse.json({ status, bookingId: "b-1" })),
    );

  it("confirms a successful payment and links to the commuter's tickets", async () => {
    mockOrder("Succeeded");

    renderWithProviders(<BookingPaymentStatusPage />, { route: "/booking/payment-return?orderId=UPTS-1", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByText("Payment confirmed. Your boarding pass is ready.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /View my tickets/ })).toHaveAttribute("href", "/my-tickets");
  });

  it("tells the commuter to wait while the payment is pending, without a tickets link", async () => {
    mockOrder("Pending");

    renderWithProviders(<BookingPaymentStatusPage />, { route: "/booking/payment-return?orderId=UPTS-1", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByText(/Waiting for payment confirmation/)).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /View my tickets/ })).not.toBeInTheDocument();
  });

  it("explains that a failed payment was not completed", async () => {
    mockOrder("Failed");

    renderWithProviders(<BookingPaymentStatusPage />, { route: "/booking/payment-return?orderId=UPTS-1", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByText(/Payment was not completed/)).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /View my tickets/ })).not.toBeInTheDocument();
  });

  it("shows the cancelled message on the cancel page without asking the API", async () => {
    // No handler is registered for the order: any request would fail the test.
    renderWithProviders(<BookingPaymentStatusPage cancelled />, { route: "/booking/payment-cancel", user: buildUser({ role: "Commuter" }) });

    expect(screen.getByText("Payment was cancelled. Your booking was not confirmed.")).toBeInTheDocument();
  });

  it("shows an error when the payment status cannot be loaded", async () => {
    server.use(http.get(apiUrl("/api/v1/payments/orders/UPTS-1"), () => HttpResponse.json({ error: "x" }, { status: 500 })));

    renderWithProviders(<BookingPaymentStatusPage />, { route: "/booking/payment-return?orderId=UPTS-1", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByText("Unable to retrieve payment status.")).toBeInTheDocument();
  });
});

describe("Commuter access in the app", () => {
  it("lets a commuter open My tickets", async () => {
    server.use(
      http.get(apiUrl("/api/v1/bookings/me"), () => HttpResponse.json([])),
    );

    renderWithProviders(<App />, { route: "/my-tickets", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByRole("heading", { name: "My tickets" })).toBeInTheDocument();
  });

  it("blocks a commuter from the staff booking and passenger pages", async () => {
    renderWithProviders(<App />, { route: "/fares/bookings", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByRole("heading", { name: "Access Restricted" })).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole("heading", { name: "My tickets" })).not.toBeInTheDocument());
  });

  it("lets a dispatcher open the staff booking page", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/passengers"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/bookings"), () => HttpResponse.json([])),
    );

    renderWithProviders(<App />, { route: "/fares/bookings", user: buildStaff("Dispatcher", "centre-1") });

    expect(await screen.findByRole("heading", { name: "Bookings" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Access Restricted" })).not.toBeInTheDocument();
  });
});
