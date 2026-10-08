import { Route, Routes } from "react-router";
import { describe, expect, it } from "vitest";
import { TripDetailPage } from "@/features/operations/TripDetailPage";
import type { TripStatus } from "@/features/operations/types/trips";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID, buildTripDetail } from "./fixtures";

const user = buildStaff("Dispatcher", CENTRE_ID);

const mockTrip = (status: TripStatus = "Scheduled") =>
  server.use(http.get(apiUrl("/api/v1/trips/trip-1"), () => HttpResponse.json(buildTripDetail({ status }))));

function renderDetail() {
  return renderWithProviders(
    <Routes>
      <Route path="/operations/dispatch/:tripId" element={<TripDetailPage />} />
      <Route path="/operations/history" element={<div>Trip history page</div>} />
    </Routes>,
    { route: "/operations/dispatch/trip-1", user },
  );
}

describe("TripDetailPage: details", () => {
  it("shows the route, bay, vehicle, driver, capacity and notes", async () => {
    mockTrip();

    renderDetail();

    expect(await screen.findByRole("heading", { name: "Trip 101" })).toBeInTheDocument();
    expect(screen.getByText("B1")).toBeInTheDocument();
    expect(screen.getByText("WP NB-4821 · Ashok Leyland Viking")).toBeInTheDocument();
    expect(screen.getByText("Kamal Perera · DL-100")).toBeInTheDocument();
    expect(screen.getByText("52 seats")).toBeInTheDocument();
    expect(screen.getByText("Hold for the connecting train")).toBeInTheDocument();
  });

  it("links to the incident form for this trip, and to the edit form while the trip is active", async () => {
    mockTrip();

    renderDetail();

    expect(await screen.findByRole("button", { name: /Report incident/ })).toHaveAttribute("href", "/operations/incidents/new?tripId=trip-1");
    expect(screen.getByRole("button", { name: /Edit/ })).toHaveAttribute("href", "/operations/dispatch/trip-1/edit");
    expect(screen.getByRole("button", { name: /Dispatch board/ })).toHaveAttribute("href", "/operations/dispatch");
  });

  it.each<[TripStatus, string]>([
    ["Scheduled", "1 of 5 operational stages"],
    ["Boarding", "3 of 5 operational stages"],
    ["Dispatched", "4 of 5 operational stages"],
    ["Completed", "Service completed"],
    ["Delayed", "Attention required before boarding"],
    ["Cancelled", "Service cancelled"],
  ])("explains a %s trip in the lifecycle panel", async (status, text) => {
    mockTrip(status);

    renderDetail();

    expect(await screen.findByText(text)).toBeInTheDocument();
  });

  it.each<[TripStatus, string]>([
    ["Scheduled", "Mark ready"],
    ["Ready", "Mark boarding"],
    ["Delayed", "Mark boarding"],
    ["Boarding", "Mark dispatched"],
    ["Dispatched", "Mark completed"],
  ])("offers the next step for a %s trip: %s", async (status, label) => {
    mockTrip(status);

    renderDetail();

    expect(await screen.findByRole("button", { name: label })).toBeEnabled();
  });

  it.each<TripStatus>(["Completed", "Cancelled"])("offers no actions on a %s trip and links back to the history", async (status) => {
    mockTrip(status);

    renderDetail();

    await screen.findByRole("heading", { name: "Trip 101" });
    expect(screen.queryByRole("button", { name: /Mark / })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Edit/ })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Cancel trip/ })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Trip history/ })).toHaveAttribute("href", "/operations/history");
  });

  it("shows a loading message and then 'Trip not found.' for an unknown trip", async () => {
    server.use(http.get(apiUrl("/api/v1/trips/trip-1"), () => HttpResponse.json({ error: "Not found" }, { status: 404 })));

    renderDetail();

    expect(screen.getByText("Loading trip...")).toBeInTheDocument();
    expect(await screen.findByText("Trip not found.")).toBeInTheDocument();
  });
});

describe("TripDetailPage: status changes and cancellation", () => {
  it("sends PATCH /api/v1/trips/{id}/status with the next status when the dispatcher marks it", async () => {
    mockTrip("Scheduled");
    let body: unknown;
    server.use(
      http.patch(apiUrl("/api/v1/trips/trip-1/status"), async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: "Mark ready" }));

    await waitFor(() => expect(body).toMatchObject({ status: "Ready" }));
  });

  it("tells the dispatcher why the API rejected a status change", async () => {
    mockTrip("Scheduled");
    server.use(
      http.patch(apiUrl("/api/v1/trips/trip-1/status"), () =>
        HttpResponse.json({ error: "Cannot change a Scheduled trip to Ready." }, { status: 400 }),
      ),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: "Mark ready" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Cannot change a Scheduled trip to Ready.");
  });

  it("requires a reason before the cancellation can be confirmed", async () => {
    mockTrip("Ready");
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: /Cancel trip/ }));
    const confirm = await screen.findByRole("button", { name: "Confirm cancellation" });
    expect(confirm).toBeDisabled();

    await person.type(screen.getByPlaceholderText("Required cancellation reason"), "   ");
    expect(confirm).toBeDisabled();
    await person.type(screen.getByPlaceholderText("Required cancellation reason"), "Road closed");
    expect(confirm).toBeEnabled();
  });

  it("sends DELETE /api/v1/trips/{id} with the reason and opens the trip history", async () => {
    mockTrip("Ready");
    let body: unknown;
    server.use(
      http.delete(apiUrl("/api/v1/trips/trip-1"), async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: /Cancel trip/ }));
    await person.type(await screen.findByPlaceholderText("Required cancellation reason"), "Road closed");
    await person.click(screen.getByRole("button", { name: "Confirm cancellation" }));

    expect(await screen.findByText("Trip history page")).toBeInTheDocument();
    expect(body).toEqual({ reason: "Road closed" });
  });

  it("keeps the dispatcher on the trip and explains why when the cancellation is refused", async () => {
    mockTrip("Ready");
    server.use(
      http.delete(apiUrl("/api/v1/trips/trip-1"), () => HttpResponse.json({ error: "Only active trips can be cancelled." }, { status: 400 })),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: /Cancel trip/ }));
    await person.type(await screen.findByPlaceholderText("Required cancellation reason"), "Road closed");
    await person.click(screen.getByRole("button", { name: "Confirm cancellation" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Only active trips can be cancelled.");
    expect(screen.queryByText("Trip history page")).not.toBeInTheDocument();
  });
});
