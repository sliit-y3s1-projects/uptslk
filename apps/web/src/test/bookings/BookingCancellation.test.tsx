import { describe, expect, it } from "vitest";
import { BookingTicket } from "@/features/fares/components/BookingTicket";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

describe("Booking cancellation", () => {
  it("requires a reason and sends it when cancelling a confirmed booking", async () => {
    let requestBody: unknown;
    server.use(
      http.get(apiUrl("/api/v1/bookings/booking-2"), () =>
        HttpResponse.json({
          id: "booking-2",
          trip: {
            id: "trip-2",
            scheduledTime: "2026-10-08T10:00:00Z",
            route: "101",
            name: "Colombo Fort",
            vehicle: "NB-1234",
          },
          passenger: {
            id: "passenger-1",
            fullName: "Nadee Perera",
            phoneNumber: "0771234567",
            category: "Adult",
            balance: 1000,
          },
          seatNumber: "A1",
          fare: 250,
          passengerCategory: "Adult",
          qrCode: "booking-2-qr",
          status: "Confirmed",
          cancelledAt: null,
          cancellationReason: null,
          refundAmount: 0,
          transactions: [],
          createdAt: "2026-10-08T08:00:00Z",
          updatedAt: "2026-10-08T08:00:00Z",
        }),
      ),
      http.get(apiUrl("/api/v1/trips/trip-2"), () =>
        HttpResponse.json({
          id: "trip-2",
          status: "Scheduled",
          bay: { code: "B2" },
          vehicle: { isAccessible: true, capacity: 40 },
        }),
      ),
      http.get(apiUrl("/api/v1/bookings/trips/trip-2/seats"), () =>
        HttpResponse.json([{ seatNumber: "A1", isAvailable: false }]),
      ),
      http.delete(apiUrl("/api/v1/bookings/booking-2"), async ({ request }) => {
        requestBody = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const { user } = renderWithProviders(<BookingTicket id="booking-2" />);

    await user.click(await screen.findByRole("button", { name: "Cancel and refund" }));
    const confirm = screen.getByRole("button", {
      name: "Confirm cancellation and refund",
    });

    expect(confirm).toBeDisabled();
    await user.type(screen.getByLabelText("Cancellation reason"), "Passenger changed plans");
    expect(confirm).toBeEnabled();
    await user.click(confirm);

    expect(requestBody).toEqual({ reason: "Passenger changed plans" });
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Booking cancelled. Refund and wallet balance refreshed.",
    );
  });
});