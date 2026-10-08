import { describe, expect, it } from "vitest";
import { CreateBooking } from "@/features/fares/components/CreateBooking";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

describe("CreateBooking", () => {
  it("selects a trip and passenger, then starts checkout with the selected values", async () => {
    let requestBody: unknown;

    server.use(
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
            capacity: 40,
            occupied: 5,
            available: 35,
            isFull: false,
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
      http.post(apiUrl("/api/v1/payments/checkout"), async ({ request }) => {
        requestBody = await request.json();
        return HttpResponse.json({
          url: "https://checkout.test/session-1",
          orderId: "UPTS-1",
        });
      }),
    );

    const { user } = renderWithProviders(<CreateBooking />, {
      user: { id: "user-1", name: "Dispatcher User", email: "dispatcher@test.com", role: "Dispatcher", centreId: "centre-1" },
    });

    await user.click(screen.getByText("Select a trip"));
    await user.click(await screen.findByRole("option", { name: /101.*Colombo Fort/ }));
    await user.click(screen.getAllByRole("combobox")[1]);
    await user.click(await screen.findByRole("option", { name: /Nadee Perera/ }));

    expect(await screen.findByText(/Fare:/)).toHaveTextContent("LKR 250.00");
    expect(screen.getByText(/Fare:/)).toHaveTextContent("LKR 250.00");
    await user.click(screen.getByRole("button", { name: "Continue to payment" }));

    expect(requestBody).toEqual({
      tripId: "trip-1",
      passengerId: "passenger-1",
      passengerCount: 1,
    });
  });
});