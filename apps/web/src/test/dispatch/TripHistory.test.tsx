import { describe, expect, it } from "vitest";
import { Route, Routes } from "react-router";
import { TripHistoryPage } from "@/features/operations/TripHistoryPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, within } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID, buildTrip } from "./fixtures";

const user = buildStaff("Dispatcher", CENTRE_ID);
const mockHistory = (trips: unknown[]) => server.use(http.get(apiUrl("/api/v1/trips/history"), () => HttpResponse.json(trips)));
const renderHistory = () =>
  renderWithProviders(
    <Routes>
      <Route path="/operations/history" element={<TripHistoryPage />} />
      <Route path="/operations/dispatch/:tripId" element={<div>Trip details page</div>} />
    </Routes>,
    { route: "/operations/history", user },
  );

describe("TripHistoryPage", () => {
  it("lists completed and cancelled trips, newest first, with passenger load", async () => {
    mockHistory([
      buildTrip({ id: "old", routeNumber: "OLD", status: "Completed", scheduledTime: "2030-06-01T04:00:00Z", occupied: 20, capacity: 50 }),
      buildTrip({ id: "new", routeNumber: "NEW", status: "Cancelled", scheduledTime: "2030-06-09T04:00:00Z", occupied: 0, capacity: 52 }),
    ]);

    renderHistory();

    await screen.findByText("Route NEW");
    const rows = screen.getAllByRole("row").slice(1);
    expect(rows[0]).toHaveTextContent("Route NEW");
    expect(rows[1]).toHaveTextContent("Route OLD");
    expect(rows[1]).toHaveTextContent("20 / 50");
    expect(within(rows[0]).getByText("Cancelled")).toBeInTheDocument();
  });

  it("narrows the list by search and by status", async () => {
    mockHistory([
      buildTrip({ id: "a", routeNumber: "AAA", status: "Completed", vehicle: "NB-1111" }),
      buildTrip({ id: "b", routeNumber: "BBB", status: "Cancelled", vehicle: "NB-2222" }),
    ]);
    const { user: person } = renderHistory();
    await screen.findByText("Route AAA");

    await person.type(screen.getByPlaceholderText("Search route, vehicle, driver or bay"), "2222");
    expect(screen.queryByText("Route AAA")).not.toBeInTheDocument();
    expect(screen.getByText("Route BBB")).toBeInTheDocument();

    await person.clear(screen.getByPlaceholderText("Search route, vehicle, driver or bay"));
    await person.click(screen.getByRole("combobox"));
    await person.click(await screen.findByRole("option", { name: "Completed" }));
    expect(screen.getByText("Route AAA")).toBeInTheDocument();
    expect(screen.queryByText("Route BBB")).not.toBeInTheDocument();
  });

  it("shows ten trips per page and moves between pages", async () => {
    mockHistory(Array.from({ length: 12 }, (_, index) => buildTrip({ id: `t${index}`, routeNumber: `R${String(index).padStart(2, "0")}`, status: "Completed", scheduledTime: `2030-06-${String(20 - index).padStart(2, "0")}T04:00:00Z` })));
    const { user: person } = renderHistory();
    await screen.findByText("Route R00");

    expect(screen.getByText("Page 1 of 2")).toBeInTheDocument();
    expect(screen.getAllByRole("row").length - 1).toBe(10);
    expect(screen.getByRole("button", { name: "Previous page" })).toBeDisabled();

    await person.click(screen.getByRole("button", { name: "Next page" }));

    expect(screen.getByText("Page 2 of 2")).toBeInTheDocument();
    expect(screen.getAllByRole("row").length - 1).toBe(2);
    expect(screen.getByRole("button", { name: "Next page" })).toBeDisabled();
  });

  it("opens the trip when its row is clicked", async () => {
    mockHistory([buildTrip({ id: "trip-55", routeNumber: "R55", status: "Completed" })]);
    const { user: person } = renderHistory();

    await person.click(await screen.findByText("Route R55"));

    expect(await screen.findByText("Trip details page")).toBeInTheDocument();
  });

  it("shows an empty message and an error message", async () => {
    mockHistory([]);
    const first = renderHistory();
    expect(screen.getByText("Loading trip history...")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Trip history" })).toBeInTheDocument();
    first.unmount();

    server.use(http.get(apiUrl("/api/v1/trips/history"), () => HttpResponse.json({ error: "x" }, { status: 500 })));
    renderHistory();
    expect(await screen.findByText("Failed to load trip history.")).toBeInTheDocument();
  });
});
