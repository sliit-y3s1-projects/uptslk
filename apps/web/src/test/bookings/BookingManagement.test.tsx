import { describe, expect, it } from "vitest";
import { TicketsPage } from "@/features/fares/FarePages";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

const passengers = [
  {
    id: "passenger-1",
    fullName: "Nadee Perera",
    phoneNumber: "0771234567",
    category: "Adult",
    balance: 1000,
    bookingCount: 1,
    isActive: true,
    email: null,
  },
];

describe("Booking management", () => {
  it("renders booking route, passenger, fare, seat, and status from the API", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/passengers"), () => HttpResponse.json(passengers)),
      http.get(apiUrl("/api/v1/bookings"), () =>
        HttpResponse.json([
          {
            id: "booking-1",
            tripId: "trip-1",
            route: "101 Colombo Fort",
            tripTime: "2026-10-08T10:00:00Z",
            passengerId: "passenger-1",
            passenger: "Nadee Perera",
            seatNumber: "A1",
            fare: 250,
            status: "Confirmed",
            createdAt: "2026-10-08T08:00:00Z",
          },
        ]),
      ),
    );

    renderWithProviders(<TicketsPage />, {
      user: buildStaff("Dispatcher", "centre-1"),
    });

    expect(await screen.findByText("101 Colombo Fort")).toBeInTheDocument();
    expect(screen.getByText("Nadee Perera")).toBeInTheDocument();
    expect(screen.getByText("A1")).toBeInTheDocument();
    expect(screen.getByText("LKR 250.00")).toBeInTheDocument();
    expect(screen.getAllByText("Confirmed").length).toBeGreaterThanOrEqual(2);
  });

  it("shows an empty state when no bookings match the filters", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/passengers"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/bookings"), () => HttpResponse.json([])),
    );

    renderWithProviders(<TicketsPage />, {
      user: buildStaff("Dispatcher", "centre-1"),
    });

    expect((await screen.findAllByText("No records match this selection.")).length).toBe(2);
  });
});