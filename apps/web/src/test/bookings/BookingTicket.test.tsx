import { describe, expect, it, vi } from "vitest";
import { BookingTicket } from "@/features/fares/components/BookingTicket";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

const cancelledBooking = {
  id: "booking-1",
  trip: {
    id: "trip-1",
    scheduledTime: "2026-10-08T10:00:00Z",
    route: "101",
    name: "Colombo Fort to Kandy",
    vehicle: "NB-1234",
  },
  passenger: {
    id: "passenger-1",
    fullName: "Nadee Perera",
    phoneNumber: "0771234567",
    category: "Adult",
    balance: 1250,
  },
  seatNumber: "A1",
  fare: 250,
  passengerCategory: "Adult",
  qrCode: "booking-1-qr",
  status: "Cancelled",
  cancelledAt: "2026-10-08T09:00:00Z",
  cancellationReason: "Passenger request",
  refundAmount: 250,
  transactions: [
    {
      id: "transaction-1",
      bookingId: "booking-1",
      type: "Refund",
      amount: 250,
      createdAt: "2026-10-08T09:00:00Z",
    },
  ],
  createdAt: "2026-10-08T08:00:00Z",
  updatedAt: "2026-10-08T09:00:00Z",
};

describe("BookingTicket", () => {
  it("renders a cancelled ticket with refund and wallet details", async () => {
    server.use(
      http.get(apiUrl("/api/v1/bookings/booking-1"), () =>
        HttpResponse.json(cancelledBooking),
      ),
      http.get(apiUrl("/api/v1/trips/trip-1"), () =>
        HttpResponse.json({
          id: "trip-1",
          status: "Scheduled",
          bay: { code: "B2" },
          vehicle: { isAccessible: true, capacity: 40 },
        }),
      ),
      http.get(apiUrl("/api/v1/bookings/trips/trip-1/seats"), () =>
        HttpResponse.json([{ seatNumber: "A1", isAvailable: false }]),
      ),
    );

    renderWithProviders(<BookingTicket id="booking-1" />);

    expect(await screen.findByRole("heading", { name: "Digital ticket" })).toBeInTheDocument();
    expect(screen.getByText("Refund recorded: LKR 250.00")).toBeInTheDocument();
    expect(screen.getByText("Reason: Passenger request")).toBeInTheDocument();
    expect(screen.getByText("Wallet balance: LKR 1,250.00")).toBeInTheDocument();
    expect(screen.getByText("Refund")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Cancel and refund" })).not.toBeInTheDocument();
  });

  it("shows a retryable error when the ticket API fails", async () => {
    const attempts = vi.fn();
    server.use(
      http.get(apiUrl("/api/v1/bookings/missing"), () => {
        attempts();
        return HttpResponse.json({ error: "Booking could not be loaded" }, { status: 404 });
      }),
    );

    const { user } = renderWithProviders(<BookingTicket id="missing" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Booking could not be loaded");
    await user.click(screen.getByRole("button", { name: "Try again" }));

    expect(attempts).toHaveBeenCalledTimes(2);
  });
});