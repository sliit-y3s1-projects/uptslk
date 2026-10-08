import { describe, expect, it, vi } from "vitest";
import { CreateBooking } from "@/features/fares/components/CreateBooking";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

describe("CreateBooking", () => {
  it("selects a trip and seat, then creates a booking with the selected values", async () => {
    let requestBody: unknown;
    const onCreated = vi.fn();

    server.use(
      http.get(apiUrl("/api/v1/centres"), () =>
        HttpResponse.json([{ id: "centre-1", name: "Colombo", code: "CMB" }]),
      ),
      http.get(apiUrl("/api/v1/trips"), () =>
        HttpResponse.json([
          {
            id: "trip-1",
            centreId: "centre-1",
            routeId: "route-1",
            routeNumber: "101",
            routeName: "Colombo Fort",
            vehicle: "NB-1234",
            bay: "B2",
            scheduledTime: "2026-10-08T10:00:00Z",
            status: "Scheduled",
          },
        ]),
      ),
      http.get(apiUrl("/api/v1/passengers"), () =>
        HttpResponse.json([
          {
            id: "passenger-1",
            fullName: "Nadee Perera",
            phoneNumber: "0771234567",
            category: "Adult",
            balance: 1000,
            bookingCount: 0,
            isActive: true,
            email: null,
          },
        ]),
      ),
      http.get(apiUrl("/api/v1/fare-rules/quote"), () =>
        HttpResponse.json({
          trip: {
            id: "trip-1",
            scheduledTime: "2026-10-08T10:00:00Z",
            route: "101",
            name: "Colombo Fort",
          },
          passenger: { id: "passenger-1", category: "Adult" },
          fareRuleId: "fare-1",
          fare: 250,
        }),
      ),
      http.get(apiUrl("/api/v1/bookings/trips/trip-1/seats"), () =>
        HttpResponse.json([{ seatNumber: "A1", isAvailable: true }]),
      ),
      http.post(apiUrl("/api/v1/bookings"), async ({ request }) => {
        requestBody = await request.json();
        return HttpResponse.json({
          id: "booking-1",
          tripId: "trip-1",
          passengerId: "passenger-1",
          seatNumber: "A1",
          fare: 250,
          status: "Confirmed",
          qrCode: "booking-1-qr",
        });
      }),
    );

    const { user } = renderWithProviders(<CreateBooking onCreated={onCreated} />);

    const centreSelect = await screen.findByRole("combobox", { name: "Centre" });
    await screen.findByRole("option", { name: "Colombo (CMB)" });
    await user.selectOptions(centreSelect, "centre-1");
    await user.selectOptions(
      await screen.findByRole("combobox", { name: "Scheduled trip" }),
      "trip-1",
    );
    await user.selectOptions(
      await screen.findByRole("combobox", { name: "Passenger" }),
      "passenger-1",
    );

    expect(await screen.findByText(/Fare:/)).toHaveTextContent("LKR 250.00");
    expect(screen.getByText(/Fare:/)).toHaveTextContent("LKR 1,000.00");
    await user.click(await screen.findByRole("button", { name: "Seat A1" }));
    await user.click(screen.getByRole("button", { name: "Confirm seat A1 and pay" }));

    expect(requestBody).toEqual({
      tripId: "trip-1",
      passengerId: "passenger-1",
      seatNumber: "A1",
    });
    expect(onCreated).toHaveBeenCalledWith("booking-1");
  });
});