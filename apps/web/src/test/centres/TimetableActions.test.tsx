import { describe, expect, it } from "vitest";
import { Route, Routes } from "react-router";
import { TimetablesPage } from "@/features/network/TimetablesPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor, within } from "@/test/render";
import { server } from "@/test/server";

const centre = { id: "centre-1", code: "CMB", name: "Colombo Fort" };
const endCentre = { id: "centre-2", code: "KDY", name: "Kandy" };
const route = {
  id: "route-1", centreId: "centre-1", routeNumber: "101", name: "Colombo - Kandy", origin: "Colombo", destination: "Kandy",
  estimatedDurationMin: 180, isActive: true,
  directions: [
    {
      id: "dir-1", routeId: "route-1", startCentreId: "centre-1", startCentre: centre, endCentreId: "centre-2", endCentre,
      name: "Colombo to Kandy", distanceKm: 115, estimatedDurationMin: 180, isActive: true, stops: [], schedules: [],
    },
  ],
};
const schedule = {
  id: "schedule-1", routeId: "route-1", routeDirectionId: "dir-1", bayId: "bay-1", bayCode: "B1",
  firstDeparture: "05:00:00", lastDeparture: "06:00:00", headwayMinutes: 30, operatingDays: "Everyday", isActive: true,
};

function mockPage(extra: Parameters<typeof server.use> = []) {
  // Overrides go first: when two handlers match the same request, MSW uses the first one.
  server.use(
    ...extra,
    http.get(apiUrl("/api/v1/routes"), () => HttpResponse.json([route])),
    http.get(apiUrl("/api/v1/routes/route-1/schedules"), () => HttpResponse.json([schedule])),
    http.get(apiUrl("/api/v1/centres/:id/bays"), () => HttpResponse.json([{ id: "bay-1", centreId: "centre-1", code: "B1", status: "Available" }])),
    http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json([])),
  );
}

function renderTimetables() {
  return renderWithProviders(
    <Routes>
      <Route path="/network/timetables" element={<TimetablesPage />} />
    </Routes>,
    { route: "/network/timetables?route=route-1", user: buildStaff("Admin", "centre-1") },
  );
}

describe("TimetablesPage actions", () => {
  it("lists the recurring timetables of the selected route and direction", async () => {
    mockPage();

    renderTimetables();

    expect(await screen.findByText("Everyday")).toBeInTheDocument();
    expect(screen.getByText(/Every 30 min · Bay B1/)).toBeInTheDocument();
    expect(screen.getByText(/05:00 – 06:00/)).toBeInTheDocument();
  });

  it("sends POST generate-trips with the service date and shows the generation summary", async () => {
    let body: { serviceDate?: string } = {};
    mockPage([
      http.post(apiUrl("/api/v1/routes/schedules/schedule-1/generate-trips"), async ({ request }) => {
        body = (await request.json()) as { serviceDate?: string };
        return HttpResponse.json({
          planned: 3, created: 2, existing: 1, conflicts: 0, serviceDate: body.serviceDate,
          createdDepartures: ["05:00", "06:00"], skippedDepartures: [],
        });
      }),
    ]);
    const { user } = renderTimetables();

    await user.click(await screen.findByRole("button", { name: /Generate trips/ }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("Generate daily trips")).toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Generate trips" }));

    expect(await screen.findByText("Trip generation summary")).toBeInTheDocument();
    expect(body.serviceDate).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(screen.getByText("Added to dispatch")).toBeInTheDocument();
    expect(screen.getByText("Departures added")).toBeInTheDocument();
    expect(screen.getByText("05:00")).toBeInTheDocument();
    expect(screen.getByText("06:00")).toBeInTheDocument();
  });

  it("tells the user when trips cannot be generated and keeps the dialog open", async () => {
    mockPage([
      http.post(apiUrl("/api/v1/routes/schedules/schedule-1/generate-trips"), () =>
        HttpResponse.json({ error: "Add at least one active vehicle and driver before generating trips." }, { status: 400 }),
      ),
    ]);
    const { user } = renderTimetables();

    await user.click(await screen.findByRole("button", { name: /Generate trips/ }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Generate trips" }));

    expect(await within(dialog).findByText(/Unable to generate trips/)).toBeInTheDocument();
    expect(screen.queryByText("Trip generation summary")).not.toBeInTheDocument();
  });

  it("sends DELETE for a timetable when it is deactivated", async () => {
    let deleted = false;
    mockPage([
      http.delete(apiUrl("/api/v1/routes/schedules/schedule-1"), () => {
        deleted = true;
        return new HttpResponse(null, { status: 204 });
      }),
    ]);
    const { user } = renderTimetables();

    await user.click(await screen.findByRole("button", { name: /Deactivate/ }));

    await waitFor(() => expect(deleted).toBe(true));
  });

  it("shows the timetable load error when the schedules request fails", async () => {
    mockPage([
      http.get(apiUrl("/api/v1/routes/route-1/schedules"), () => HttpResponse.json({ error: "x" }, { status: 500 })),
    ]);

    renderTimetables();

    expect(await screen.findByText("Could not load timetable data. Please try again.")).toBeInTheDocument();
  });
});
