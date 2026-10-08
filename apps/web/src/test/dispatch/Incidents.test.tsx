import { Route, Routes } from "react-router";
import { describe, expect, it } from "vitest";
import { IncidentFormPage } from "@/features/operations/IncidentFormPage";
import { IncidentsPage } from "@/features/operations/IncidentsPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor, within } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID, buildIncident, buildTrip } from "./fixtures";

const user = buildStaff("Dispatcher", CENTRE_ID);
const mockIncidents = (items: unknown[]) => server.use(http.get(apiUrl("/api/v1/incidents"), () => HttpResponse.json(items)));
const group = async (name: string) => within((await screen.findByRole("heading", { name })).closest("article")!);

describe("IncidentsPage", () => {
  it("groups incidents as Open, Investigating and Resolved with counts", async () => {
    mockIncidents([
      buildIncident({ id: "i1", title: "Open one", status: "Open" }),
      buildIncident({ id: "i2", title: "Open two", status: "Open" }),
      buildIncident({ id: "i3", title: "Being checked", status: "InProgress" }),
      buildIncident({ id: "i4", title: "All done", status: "Resolved" }),
    ]);

    renderWithProviders(<IncidentsPage />, { route: "/operations/incidents", user });

    const open = await group("Open");
    expect(open.getByText("Open one")).toBeInTheDocument();
    expect(open.getByText("Open two")).toBeInTheDocument();
    expect((await group("Investigating")).getByText("Being checked")).toBeInTheDocument();
    expect((await group("Resolved")).getByText("All done")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Open" }).closest("header")).toHaveTextContent("2");
  });

  it("shows the route of a trip incident, 'Centre-wide incident' otherwise, and who owns it", async () => {
    mockIncidents([
      buildIncident({ id: "i1", title: "Trip issue", assignedTo: "Dina" }),
      buildIncident({ id: "i2", title: "General issue", tripId: undefined, tripRouteNumber: undefined, tripRouteName: undefined }),
    ]);

    renderWithProviders(<IncidentsPage />, { route: "/operations/incidents", user });

    expect(await screen.findByText("Route 101 · Colombo - Kandy")).toBeInTheDocument();
    expect(screen.getByText("Centre-wide incident")).toBeInTheDocument();
    expect(screen.getByText(/Dina/)).toBeInTheDocument();
    expect(screen.getByText(/Control room/)).toBeInTheDocument();
  });

  it("starts an investigation with PUT /api/v1/incidents/{id}, keeping the other fields", async () => {
    mockIncidents([buildIncident({ id: "i1", status: "Open", severity: "High", title: "Engine fault at bay 3" })]);
    let received: { id?: string; body?: unknown } = {};
    server.use(
      http.put(apiUrl("/api/v1/incidents/:id"), async ({ params, request }) => {
        received = { id: String(params.id), body: await request.json() };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderWithProviders(<IncidentsPage />, { route: "/operations/incidents", user });

    await person.click(await screen.findByRole("button", { name: "Begin investigation" }));

    await waitFor(() => expect(received.id).toBe("i1"));
    expect(received.body).toMatchObject({ status: "InProgress", severity: "High", title: "Engine fault at bay 3", description: "The bus stopped while boarding." });
  });

  it("resolves an investigated incident, and offers no action on a resolved one", async () => {
    mockIncidents([
      buildIncident({ id: "i1", title: "Investigated", status: "InProgress" }),
      buildIncident({ id: "i2", title: "Finished", status: "Resolved" }),
    ]);
    let body: unknown;
    server.use(
      http.put(apiUrl("/api/v1/incidents/i1"), async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderWithProviders(<IncidentsPage />, { route: "/operations/incidents", user });

    expect(within((await group("Resolved")).getByText("Finished").closest("div.rounded-md")! as HTMLElement).queryByRole("button")).not.toBeInTheDocument();
    await person.click(await screen.findByRole("button", { name: "Resolve incident" }));

    await waitFor(() => expect(body).toMatchObject({ status: "Resolved" }));
  });

  it("tells the dispatcher when an incident update is rejected", async () => {
    mockIncidents([buildIncident({ id: "i1", status: "Open" })]);
    server.use(http.put(apiUrl("/api/v1/incidents/i1"), () => HttpResponse.json({ error: "Incident not found." }, { status: 404 })));
    const { user: person } = renderWithProviders(<IncidentsPage />, { route: "/operations/incidents", user });

    await person.click(await screen.findByRole("button", { name: "Begin investigation" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Incident not found.");
  });

  it("shows a loading message and an error message when the incidents cannot be loaded", async () => {
    server.use(http.get(apiUrl("/api/v1/incidents"), () => HttpResponse.json({ error: "x" }, { status: 500 })));

    renderWithProviders(<IncidentsPage />, { route: "/operations/incidents", user });

    expect(screen.getByText("Loading incidents...")).toBeInTheDocument();
    expect(await screen.findByText("Failed to load incidents.")).toBeInTheDocument();
  });
});

describe("IncidentFormPage", () => {
  const mockTrips = () => server.use(http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json([buildTrip({ id: "trip-1" }), buildTrip({ id: "trip-2", routeNumber: "202" })])));
  const renderForm = (route = "/operations/incidents/new") =>
    renderWithProviders(
      <Routes>
        <Route path="/operations/incidents/new" element={<IncidentFormPage />} />
        <Route path="/operations/incidents" element={<div>Incidents list page</div>} />
      </Routes>,
      { route, user },
    );

  it("requires a summary and details before it can be submitted", async () => {
    mockTrips();
    let posted = false;
    server.use(http.post(apiUrl("/api/v1/incidents"), () => { posted = true; return HttpResponse.json({ id: "x" }); }));
    const { user: person } = renderForm();
    await screen.findByRole("heading", { name: "Report incident" });

    await person.click(screen.getByRole("button", { name: "Report incident" }));

    expect(screen.getByPlaceholderText("Describe the operational issue")).toBeInvalid();
    expect(screen.getByPlaceholderText("Add operational context")).toBeInvalid();
    expect(posted).toBe(false);
  });

  it("sends POST /api/v1/incidents linked to the trip in the link, then returns to the incident list", async () => {
    mockTrips();
    let body: Record<string, unknown> = {};
    server.use(
      http.post(apiUrl("/api/v1/incidents"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ id: "new-incident" });
      }),
    );
    const { user: person } = renderForm("/operations/incidents/new?tripId=trip-2");

    await person.type(await screen.findByPlaceholderText("Describe the operational issue"), "Door will not close");
    await person.type(screen.getByPlaceholderText("Add operational context"), "Driver reports a jammed rear door");
    await person.click(screen.getByRole("button", { name: "Report incident" }));

    expect(await screen.findByText("Incidents list page")).toBeInTheDocument();
    expect(body).toMatchObject({
      centreId: CENTRE_ID, tripId: "trip-2", reportedByName: "Dispatcher User", type: "Delay", severity: "Medium",
      title: "Door will not close", description: "Driver reports a jammed rear door",
    });
  });

  it("can report a centre-wide incident without a trip", async () => {
    mockTrips();
    let body: Record<string, unknown> = {};
    server.use(
      http.post(apiUrl("/api/v1/incidents"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ id: "new-incident" });
      }),
    );
    const { user: person } = renderForm();
    await screen.findByRole("heading", { name: "Report incident" });

    await person.click(screen.getByRole("combobox", { name: /Related trip/ }));
    await person.click(await screen.findByRole("option", { name: "Centre-wide incident" }));
    await person.type(screen.getByPlaceholderText("Describe the operational issue"), "Power cut at the terminal");
    await person.type(screen.getByPlaceholderText("Add operational context"), "Lights and gates are off");
    await person.click(screen.getByRole("button", { name: "Report incident" }));

    await screen.findByText("Incidents list page");
    expect(body.tripId).toBeUndefined();
  });

  it("tells the dispatcher when the incident cannot be saved and stays on the form", async () => {
    mockTrips();
    server.use(http.post(apiUrl("/api/v1/incidents"), () => HttpResponse.json({ error: "The selected trip does not belong to this centre." }, { status: 400 })));
    const { user: person } = renderForm();
    await person.type(await screen.findByPlaceholderText("Describe the operational issue"), "Door will not close");
    await person.type(screen.getByPlaceholderText("Add operational context"), "Jammed door");

    await person.click(screen.getByRole("button", { name: "Report incident" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("The selected trip does not belong to this centre.");
    expect(screen.queryByText("Incidents list page")).not.toBeInTheDocument();
  });
});
