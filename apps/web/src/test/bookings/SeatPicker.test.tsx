import { describe, expect, it, vi } from "vitest";
import { SeatPicker } from "@/features/fares/components/SeatPicker";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

describe("SeatPicker", () => {
  it("loads seats, disables unavailable seats, and reports the selected seat", async () => {
    const onChange = vi.fn();

    server.use(
      http.get(apiUrl("/api/v1/bookings/trips/trip-1/seats"), () =>
        HttpResponse.json([
          { seatNumber: "A1", isAvailable: true },
          { seatNumber: "A2", isAvailable: false },
        ]),
      ),
    );

    const { user } = renderWithProviders(
      <SeatPicker tripId="trip-1" value="A1" onChange={onChange} />,
    );

    const selectedSeat = await screen.findByRole("button", { name: "Seat A1" });
    const unavailableSeat = screen.getByRole("button", {
      name: "Seat A2, unavailable",
    });

    expect(selectedSeat).toHaveAttribute("aria-pressed", "true");
    expect(unavailableSeat).toBeDisabled();
    expect(screen.getByText("1 available · 2 seats total. Unavailable seats are disabled.")).toBeInTheDocument();

    await user.click(selectedSeat);
    expect(onChange).toHaveBeenCalledWith("A1");
  });

  it("shows an API error and lets the user retry", async () => {
    let attempts = 0;
    server.use(
      http.get(apiUrl("/api/v1/bookings/trips/trip-2/seats"), () => {
        attempts += 1;
        return HttpResponse.json({ error: "Seat service unavailable" }, { status: 503 });
      }),
    );

    const { user } = renderWithProviders(
      <SeatPicker tripId="trip-2" value="" onChange={vi.fn()} />,
    );

    expect(await screen.findByRole("alert")).toHaveTextContent("Seat service unavailable");
    await user.click(screen.getByRole("button", { name: "Try again" }));

    expect(attempts).toBe(2);
    expect(await screen.findByRole("alert")).toHaveTextContent("Seat service unavailable");
  });
});