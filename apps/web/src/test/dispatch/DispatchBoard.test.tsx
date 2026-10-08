import { describe, expect, it } from "vitest";
import { DispatchPage } from "@/features/operations/DispatchPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor, within } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID, buildTrip, tripWith } from "./fixtures";

const user = buildStaff("Dispatcher", CENTRE_ID);

const mockTrips = (trips: unknown[]) =>
  server.use(http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json(trips)));

describe("DispatchPage (dispatch board)", () => {
  it("places each active trip in the column for its status and shows route, vehicle, bay and driver", async () => {
    mockTrips([
      tripWith("Delayed", { routeNumber: "D1", vehicle: "NB-DELAYED" }),
      tripWith("Boarding", { routeNumber: "B1", vehicle: "NB-BOARDING" }),
      tripWith("Scheduled", { routeNumber: "S1", vehicle: "NB-SCHEDULED" }),
      tripWith("Ready", { routeNumber: "R1", vehicle: "NB-READY" }),
      tripWith("Dispatched", { routeNumber: "X1", vehicle: "NB-DISPATCHED" }),
    ]);

    renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    const column = async (name: string) => (await screen.findByRole("heading", { name })).closest("section")!;
    expect(within(await column("Attention")).getByText("NB-DELAYED")).toBeInTheDocument();
    expect(within(await column("Boarding")).getByText("NB-BOARDING")).toBeInTheDocument();
    const ready = within(await column("Ready & scheduled"));
    expect(ready.getByText("NB-SCHEDULED")).toBeInTheDocument();
    expect(ready.getByText("NB-READY")).toBeInTheDocument();
    expect(within(await column("Dispatched")).getByText("NB-DISPATCHED")).toBeInTheDocument();
    expect(screen.getAllByText("Kamal Perera").length).toBe(5);
    expect(screen.getAllByText("B1").length).toBe(5);
  });

  it("does not show completed or cancelled trips on the board", async () => {
    mockTrips([tripWith("Scheduled", { vehicle: "NB-ACTIVE" }), tripWith("Completed", { vehicle: "NB-DONE" }), tripWith("Cancelled", { vehicle: "NB-CANCELLED" })]);

    renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    expect(await screen.findByText("NB-ACTIVE")).toBeInTheDocument();
    expect(screen.queryByText("NB-DONE")).not.toBeInTheDocument();
    expect(screen.queryByText("NB-CANCELLED")).not.toBeInTheDocument();
  });

  it("narrows the board when searching by vehicle", async () => {
    mockTrips([tripWith("Scheduled", { vehicle: "NB-1111" }), tripWith("Ready", { vehicle: "NB-2222" })]);
    const { user: person } = renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });
    await screen.findByText("NB-1111");

    await person.type(screen.getByPlaceholderText(/Search trip, route, vehicle, driver, or bay/), "2222");

    expect(screen.queryByText("NB-1111")).not.toBeInTheDocument();
    expect(screen.getByText("NB-2222")).toBeInTheDocument();
  });

  it("links each card to its trip details and offers to schedule a new trip", async () => {
    mockTrips([buildTrip({ id: "trip-77" })]);

    renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    expect(await screen.findByRole("link", { name: /Trip details/ })).toHaveAttribute("href", "/operations/dispatch/trip-77");
    expect(screen.getByRole("button", { name: /Schedule trip/ })).toHaveAttribute("href", "/operations/dispatch/new");
  });

  it("starts boarding for the selected trip with PATCH /api/v1/trips/{id}/status", async () => {
    mockTrips([buildTrip({ id: "trip-9", status: "Ready" })]);
    let received: { id?: string; body?: unknown } = {};
    server.use(
      http.patch(apiUrl("/api/v1/trips/:id/status"), async ({ params, request }) => {
        received = { id: String(params.id), body: await request.json() };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    await person.click(await screen.findByText(/Route 101/));
    await person.click(screen.getByRole("button", { name: "Start boarding" }));

    await waitFor(() => expect(received.id).toBe("trip-9"));
    expect(received.body).toMatchObject({ status: "Boarding" });
  });

  it("only allows dispatching a bus that is boarding", async () => {
    mockTrips([buildTrip({ id: "trip-ready", status: "Ready", routeNumber: "R1" }), buildTrip({ id: "trip-boarding", status: "Boarding", routeNumber: "B1" })]);
    let body: unknown;
    server.use(
      http.patch(apiUrl("/api/v1/trips/trip-boarding/status"), async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    await person.click(await screen.findByText(/Route R1/));
    expect(screen.getByRole("button", { name: /Dispatch bus/ })).toBeDisabled();
    await person.click(screen.getByText(/Route B1/));
    expect(screen.getByRole("button", { name: /Dispatch bus/ })).toBeEnabled();
    await person.click(screen.getByRole("button", { name: /Dispatch bus/ }));

    await waitFor(() => expect(body).toMatchObject({ status: "Dispatched" }));
  });

  it("does not offer Flag delay or boarding actions that the trip's state forbids", async () => {
    mockTrips([buildTrip({ id: "trip-out", status: "Dispatched" })]);
    const { user: person } = renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    await person.click(await screen.findByText(/Route 101/));

    expect(screen.getByRole("button", { name: /Flag delay/ })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Start boarding" })).toBeDisabled();
  });

  it("shows a loading message, then an empty board when there are no trips", async () => {
    mockTrips([]);

    renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    expect(screen.getByText("Loading dispatch board...")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Dispatch control" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Attention" }).closest("section")).toHaveTextContent("0");
  });

  it("shows an error message when the trips cannot be loaded", async () => {
    server.use(http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json({ error: "boom" }, { status: 500 })));

    renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    expect(await screen.findByText("Failed to load dispatch data.")).toBeInTheDocument();
  });

  it("tells the dispatcher why the API rejected a status change", async () => {
    mockTrips([buildTrip({ id: "trip-9", status: "Ready" })]);
    server.use(
      http.patch(apiUrl("/api/v1/trips/trip-9/status"), () =>
        HttpResponse.json({ error: "Cannot change a Ready trip to Boarding." }, { status: 400 }),
      ),
    );
    const { user: person } = renderWithProviders(<DispatchPage />, { route: "/operations/dispatch", user });

    await person.click(await screen.findByText(/Route 101/));
    await person.click(screen.getByRole("button", { name: "Start boarding" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Cannot change a Ready trip to Boarding.");
  });
});
