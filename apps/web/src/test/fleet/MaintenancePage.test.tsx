import { Route, Routes } from "react-router";
import { describe, expect, it, vi } from "vitest";
import {
  MaintenanceDetailPage,
  MaintenancePage,
} from "@/features/fleet/MaintenancePage";
import type {
  CentreOption,
  MaintenanceDetail,
  MaintenanceListItem,
} from "@/features/fleet/types";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

const CENTRE_ID = "00000000-0000-4000-8000-0000000000c1";
const VEHICLE_ID = "00000000-0000-4000-8000-0000000000b1";
const RECORD_ID = "00000000-0000-4000-8000-0000000000d1";

const mockCentres: CentreOption[] = [
  {
    id: CENTRE_ID,
    code: "CMB",
    name: "Colombo Central",
    city: "Colombo",
    district: "Colombo",
    status: "Operating",
    bayCount: 8,
    routeCount: 12,
  },
];

const mockRecords: MaintenanceListItem[] = [
  {
    id: RECORD_ID,
    vehicleId: VEHICLE_ID,
    vehicle: "WP NB-4821",
    centreId: CENTRE_ID,
    centre: "Colombo Central",
    type: "Engine oil service",
    description: "Replace engine oil and primary filter",
    status: "Scheduled",
    scheduledFor: "2026-10-20T03:30:00.000Z",
    completedAt: null,
  },
  {
    id: "00000000-0000-4000-8000-0000000000d2",
    vehicleId: "00000000-0000-4000-8000-0000000000b2",
    vehicle: "WP ND-9012",
    centreId: CENTRE_ID,
    centre: "Colombo Central",
    type: "Annual brake overhaul",
    description: "Full air brake pressure test",
    status: "Completed",
    scheduledFor: "2026-10-01T04:00:00.000Z",
    completedAt: "2026-10-01T08:00:00.000Z",
  },
];

const mockRecordDetail: MaintenanceDetail = {
  id: RECORD_ID,
  vehicleId: VEHICLE_ID,
  vehicle: {
    id: VEHICLE_ID,
    plateNumber: "WP NB-4821",
    model: "Ashok Leyland Viking",
    centreId: CENTRE_ID,
  },
  type: "Engine oil service",
  description: "Replace engine oil and primary filter",
  status: "Scheduled",
  scheduledFor: "2026-10-20T03:30:00.000Z",
  completedAt: null,
};

describe("MaintenancePage and MaintenanceDetailPage", () => {
  it("MaintenancePage renders scheduled and completed maintenance records and filters them by the search box", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/maintenance-records"), () =>
        HttpResponse.json(mockRecords),
      ),
    );

    const { user } = renderWithProviders(<MaintenancePage />, {
      route: "/fleet/maintenance",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(
      await screen.findByText("Engine oil service"),
    ).toBeInTheDocument();
    expect(screen.getByText("Annual brake overhaul")).toBeInTheDocument();
    expect(screen.getByText("2 maintenance records")).toBeInTheDocument();

    const searchInput = screen.getByPlaceholderText(
      "Search vehicle or maintenance type",
    );
    await user.type(searchInput, "brake");

    expect(
      screen.queryByText("Engine oil service"),
    ).not.toBeInTheDocument();
    expect(screen.getByText("Annual brake overhaul")).toBeInTheDocument();
    expect(screen.getByText("1 maintenance record")).toBeInTheDocument();
  });

  it("MaintenanceDetailPage renders the maintenance record reference, vehicle details, schedule timestamps, and description", async () => {
    server.use(
      http.get(apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`), () =>
        HttpResponse.json(mockRecordDetail),
      ),
    );

    renderWithProviders(
      <Routes>
        <Route
          path="/fleet/maintenance/:recordId"
          element={<MaintenanceDetailPage />}
        />
      </Routes>,
      {
        route: `/fleet/maintenance/${RECORD_ID}`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Engine oil service" }),
    ).toBeInTheDocument();
    expect(screen.getByText(RECORD_ID)).toBeInTheDocument();
    expect(
      screen.getByText("WP NB-4821 (Ashok Leyland Viking)"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Replace engine oil and primary filter"),
    ).toBeInTheDocument();
    expect(screen.getByText("Not completed")).toBeInTheDocument();
  });

  it("MaintenanceDetailPage sends DELETE /api/v1/maintenance-records/:recordId when cancellation is confirmed and navigates back to /fleet/maintenance", async () => {
    let cancelledRecordId: string | null = null;
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);

    server.use(
      http.get(apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`), () =>
        HttpResponse.json(mockRecordDetail),
      ),
      http.delete(
        apiUrl("/api/v1/maintenance-records/:recordId"),
        ({ params }) => {
          cancelledRecordId = String(params.recordId);
          return new HttpResponse(null, { status: 204 });
        },
      ),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/maintenance-records"), () =>
        HttpResponse.json([]),
      ),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/maintenance/:recordId"
          element={<MaintenanceDetailPage />}
        />
        <Route path="/fleet/maintenance" element={<MaintenancePage />} />
      </Routes>,
      {
        route: `/fleet/maintenance/${RECORD_ID}`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    const cancelBtn = await screen.findByRole("button", {
      name: "Cancel maintenance",
    });
    await user.click(cancelBtn);

    expect(confirmSpy).toHaveBeenCalled();
    expect(cancelledRecordId).toBe(RECORD_ID);
    expect(
      await screen.findByRole("heading", { name: "Maintenance" }),
    ).toBeInTheDocument();

    confirmSpy.mockRestore();
  });

  it("MaintenancePage shows an empty state when no records exist and an error banner when GET /api/v1/maintenance-records returns 500", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/maintenance-records"), () =>
        HttpResponse.json([]),
      ),
    );

    const { unmount } = renderWithProviders(<MaintenancePage />, {
      route: "/fleet/maintenance",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(
      await screen.findByText("No maintenance records scheduled yet."),
    ).toBeInTheDocument();

    unmount();

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/maintenance-records"), () =>
        HttpResponse.json(
          { error: "Maintenance service unavailable" },
          { status: 500 },
        ),
      ),
    );

    renderWithProviders(<MaintenancePage />, {
      route: "/fleet/maintenance",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(
      await screen.findByText(
        "Failed to load maintenance records: Maintenance service unavailable",
      ),
    ).toBeInTheDocument();
  });
});

