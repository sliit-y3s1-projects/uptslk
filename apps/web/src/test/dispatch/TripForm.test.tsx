import { Route, Routes } from "react-router";
import { describe, expect, it } from "vitest";
import { TripFormPage } from "@/features/operations/TripFormPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor, within } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID, buildTripDetail } from "./fixtures";

const user = buildStaff("Dispatcher", CENTRE_ID);
const colombo = { id: CENTRE_ID, code: "CMB", name: "Colombo Fort" };
const kandy = { id: "centre-2", code: "KDY", name: "Kandy" };

const route = {
  id: "route-1", centreId: CENTRE_ID, routeNumber: "101", name: "Colombo - Kandy", origin: "Colombo", destination: "Kandy",
  estimatedDurationMin: 180, isActive: true,
  directions: [
    {
      id: "dir-1", routeId: "route-1", startCentreId: CENTRE_ID, startCentre: colombo, endCentreId: "centre-2", endCentre: kandy,
      name: "Colombo to Kandy", distanceKm: 115, estimatedDurationMin: 180, isActive: true, stops: [], schedules: [],
    },
  ],
};

function mockLookups(extra: Parameters<typeof server.use> = []) {
  // Overrides first: when two handlers match, MSW uses the first one.
  server.use(
    ...extra,
    http.get(apiUrl("/api/v1/routes"), () => HttpResponse.json([route])),
    http.get(apiUrl("/api/v1/vehicles"), () =>
      HttpResponse.json([{ id: "vehicle-1", centreId: CENTRE_ID, plateNumber: "WP NB-4821", model: "Viking", capacity: 52, status: "Active", isAccessible: true }]),
    ),
    http.get(apiUrl("/api/v1/drivers"), () =>
      HttpResponse.json([{ id: "driver-1", centreId: CENTRE_ID, fullName: "Kamal Perera", licenseNumber: "DL-100", status: "Active" }]),
    ),
    http.get(apiUrl("/api/v1/centres/:id/bays"), () =>
      HttpResponse.json([
        { id: "bay-1", centreId: CENTRE_ID, code: "B1", status: "Available" },
        { id: "bay-2", centreId: CENTRE_ID, code: "B2", status: "OutOfService" },
      ]),
    ),
  );
}

function renderCreate() {
  return renderWithProviders(
    <Routes>
      <Route path="/operations/dispatch/new" element={<TripFormPage />} />
      <Route path="/operations/dispatch/:tripId" element={<div>Trip details page</div>} />
    </Routes>,
    { route: "/operations/dispatch/new", user },
  );
}

function renderEdit() {
  return renderWithProviders(
    <Routes>
      <Route path="/operations/dispatch/:tripId/edit" element={<TripFormPage />} />
      <Route path="/operations/dispatch/:tripId" element={<div>Trip details page</div>} />
    </Routes>,
    { route: "/operations/dispatch/trip-1/edit", user },
  );
}

describe("TripFormPage: create", () => {
  it("shows the schedule form with the first route, direction, vehicle and driver already chosen", async () => {
    mockLookups();

    renderCreate();

    expect(await screen.findByRole("heading", { name: "Schedule trip" })).toBeInTheDocument();
    expect(await screen.findByText("101 · Colombo - Kandy")).toBeInTheDocument();
    expect(await screen.findByText("Colombo Fort → Kandy")).toBeInTheDocument();
    expect(await screen.findByText("WP NB-4821 · Viking")).toBeInTheDocument();
    expect(await screen.findByText("Kamal Perera · DL-100")).toBeInTheDocument();
  });

  it("only offers bays that are available", async () => {
    mockLookups();
    const { user: person } = renderCreate();
    await screen.findByText("101 · Colombo - Kandy");

    await person.click(await screen.findByRole("combobox", { name: /Bay/ }));

    const options = await screen.findAllByRole("option");
    expect(options.map((option) => option.textContent)).toEqual(["B1"]);
  });

  it("does not save a trip without a service date and departure time, and tells the dispatcher", async () => {
    mockLookups();
    let posted = false;
    server.use(
      http.post(apiUrl("/api/v1/trips"), () => {
        posted = true;
        return HttpResponse.json({ id: "new-trip" });
      }),
    );
    const { user: person } = renderCreate();
    await screen.findByText("101 · Colombo - Kandy");

    await person.click(screen.getByRole("button", { name: "Schedule trip" }));

    expect(await screen.findByText("Select a service date and a departure time.")).toBeInTheDocument();
    expect(posted).toBe(false);
  });

  it("sends POST /api/v1/trips with the chosen date, time and resources, then opens the new trip", async () => {
    mockLookups();
    let body: Record<string, unknown> = {};
    server.use(
      http.post(apiUrl("/api/v1/trips"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ id: "new-trip" });
      }),
    );
    const { user: person } = renderCreate();
    await screen.findByText("101 · Colombo - Kandy");

    await person.click(screen.getByText("Select service date"));
    const grid = await screen.findByRole("grid");
    await person.click(within(grid).getAllByRole("button").find((day) => !day.hasAttribute("disabled"))!);
    await person.click(screen.getByText("Select departure time"));
    const hours = (await screen.findByText("Hour")).parentElement!;
    await person.click(within(hours).getByRole("button", { name: "09" }));
    await person.click(within(screen.getByText("Minute").parentElement!).getByRole("button", { name: "30" }));
    await person.type(screen.getByPlaceholderText("Optional dispatch instructions"), "  Hold for the train ");
    await person.click(screen.getByRole("button", { name: "Schedule trip" }));

    expect(await screen.findByText("Trip details page")).toBeInTheDocument();
    expect(body).toMatchObject({
      centreId: CENTRE_ID, routeId: "route-1", routeDirectionId: "dir-1", vehicleId: "vehicle-1", driverId: "driver-1", bayId: "bay-1",
    });
    const departure = new Date(String(body.scheduledTime));
    expect([departure.getHours(), departure.getMinutes()]).toEqual([9, 30]);
    expect(String(body.notes).trim()).toBe("Hold for the train");
  });
});

describe("TripFormPage: edit", () => {
  const mockTrip = () => server.use(http.get(apiUrl("/api/v1/trips/trip-1"), () => HttpResponse.json(buildTripDetail())));

  it("loads the existing trip and shows its notes", async () => {
    mockLookups();
    mockTrip();

    renderEdit();

    expect(await screen.findByRole("heading", { name: "Edit trip" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Optional dispatch instructions")).toHaveValue("Hold for the connecting train");
    expect(screen.getByRole("button", { name: "Save trip" })).toBeInTheDocument();
  });

  it("sends PUT /api/v1/trips/{id} with the changed notes and returns to the trip", async () => {
    mockLookups();
    mockTrip();
    let body: Record<string, unknown> = {};
    server.use(
      http.put(apiUrl("/api/v1/trips/trip-1"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderEdit();
    const notes = await screen.findByPlaceholderText("Optional dispatch instructions");

    await person.clear(notes);
    await person.type(notes, "Late vehicle swap");
    await person.click(screen.getByRole("button", { name: "Save trip" }));

    expect(await screen.findByText("Trip details page")).toBeInTheDocument();
    expect(body).toMatchObject({ routeId: "route-1", vehicleId: "vehicle-1", driverId: "driver-1", bayId: "bay-1", notes: "Late vehicle swap" });
    expect(new Date(String(body.scheduledTime)).toISOString()).toBe("2030-06-10T04:30:00.000Z");
  });

  it("shows every conflict the API reports (vehicle, driver, bay) when the trip clashes", async () => {
    mockLookups();
    mockTrip();
    server.use(
      http.put(apiUrl("/api/v1/trips/trip-1"), () =>
        HttpResponse.json(
          { errors: ["Vehicle is already assigned to route 101 at this time.", "Driver is already assigned to route 101 at this time."] },
          { status: 400 },
        ),
      ),
    );
    const { user: person } = renderEdit();
    await screen.findByRole("heading", { name: "Edit trip" });

    await person.click(screen.getByRole("button", { name: "Save trip" }));

    expect(await screen.findByText(/Vehicle is already assigned to route 101 at this time\./)).toBeInTheDocument();
    expect(screen.getByText(/Driver is already assigned to route 101 at this time\./)).toBeInTheDocument();
    expect(screen.queryByText("Trip details page")).not.toBeInTheDocument();
  });

  it("shows the API message and stays on the form when the API refuses the change", async () => {
    mockLookups();
    mockTrip();
    server.use(
      http.put(apiUrl("/api/v1/trips/trip-1"), () => HttpResponse.json({ error: "Completed or cancelled trips cannot be edited." }, { status: 400 })),
    );
    const { user: person } = renderEdit();
    await screen.findByRole("heading", { name: "Edit trip" });

    await person.click(screen.getByRole("button", { name: "Save trip" }));

    expect(await screen.findByText("Completed or cancelled trips cannot be edited.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save trip" })).toBeEnabled();
  });

  it("shows a loading message, and 'Trip not found.' when the trip does not exist", async () => {
    mockLookups();
    server.use(http.get(apiUrl("/api/v1/trips/trip-1"), () => HttpResponse.json({ error: "Not found" }, { status: 404 })));

    renderEdit();

    expect(screen.getByText("Loading trip...")).toBeInTheDocument();
    await waitFor(() => expect(screen.getByText("Trip not found.")).toBeInTheDocument());
  });
});
