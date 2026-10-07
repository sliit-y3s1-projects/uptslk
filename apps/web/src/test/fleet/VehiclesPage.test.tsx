import { Route, Routes } from "react-router";
import { describe, expect, it, vi } from "vitest";
import {
  VehicleProfilePage,
  VehiclesPage,
} from "@/features/fleet/VehiclesPage";
import type {
  CentreOption,
  VehicleDetail,
  VehicleListItem,
} from "@/features/fleet/types";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";

const CENTRE_ID = "00000000-0000-4000-8000-0000000000c1";
const OTHER_CENTRE_ID = "00000000-0000-4000-8000-0000000000c2";
const VEHICLE_ID = "00000000-0000-4000-8000-0000000000b1";

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

const mockVehicles: VehicleListItem[] = [
  {
    id: VEHICLE_ID,
    centreId: CENTRE_ID,
    centre: "Colombo Central",
    plateNumber: "WP NB-4821",
    model: "Ashok Leyland Viking",
    type: "SemiLuxury",
    capacity: 52,
    isAccessible: true,
    status: "Active",
    imageUrl: null,
    maintenanceCount: 0,
  },
  {
    id: "00000000-0000-4000-8000-0000000000b2",
    centreId: CENTRE_ID,
    centre: "Colombo Central",
    plateNumber: "WP ND-9012",
    model: "Yutong ZK6107",
    type: "AcExpress",
    capacity: 45,
    isAccessible: false,
    status: "Maintenance",
    imageUrl: null,
    maintenanceCount: 1,
  },
];

const mockVehicleDetail: VehicleDetail = {
  id: VEHICLE_ID,
  centreId: CENTRE_ID,
  centre: {
    id: CENTRE_ID,
    code: "CMB",
    name: "Colombo Central",
  },
  plateNumber: "WP NB-4821",
  model: "Ashok Leyland Viking",
  type: "SemiLuxury",
  capacity: 52,
  isAccessible: true,
  status: "Active",
  imageUrl: null,
  maintenance: [
    {
      id: "00000000-0000-4000-8000-0000000000a1",
      type: "Brake inspection",
      description: "Front and rear drum lining replacement",
      status: "Scheduled",
      scheduledFor: "2026-10-15T09:00:00.000Z",
      completedAt: null,
    },
  ],
};

describe("VehiclesPage and VehicleProfilePage", () => {
  it("VehiclesPage renders the list of vehicles with plate number, model, capacity, accessibility, centre, and status from the API", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
    );

    renderWithProviders(<VehiclesPage />, {
      route: "/fleet/vehicles",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(await screen.findByText("WP NB-4821")).toBeInTheDocument();
    expect(screen.getByText("Ashok Leyland Viking")).toBeInTheDocument();
    expect(
      screen.getByText(/SemiLuxury · 52 seats · Accessible/),
    ).toBeInTheDocument();
    expect(screen.getByText("Ready for service")).toBeInTheDocument();

    expect(screen.getByText("WP ND-9012")).toBeInTheDocument();
    expect(screen.getByText("Yutong ZK6107")).toBeInTheDocument();
    expect(screen.getByText("1 in maintenance")).toBeInTheDocument();
    expect(
      screen.getByText("2 vehicles assigned to Colombo Central"),
    ).toBeInTheDocument();
  });

  it("VehiclesPage narrows the list when searching by registration or model", async () => {
    const searchParamsSeen: string[] = [];

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), ({ request }) => {
        const url = new URL(request.url);
        const search = url.searchParams.get("search") ?? "";
        searchParamsSeen.push(search);
        const filtered = search
          ? mockVehicles.filter(
              (v) =>
                v.plateNumber.toLowerCase().includes(search.toLowerCase()) ||
                v.model.toLowerCase().includes(search.toLowerCase()),
            )
          : mockVehicles;
        return HttpResponse.json(filtered);
      }),
    );

    const { user } = renderWithProviders(<VehiclesPage />, {
      route: "/fleet/vehicles",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(await screen.findByText("WP NB-4821")).toBeInTheDocument();
    expect(screen.getByText("WP ND-9012")).toBeInTheDocument();

    const searchInput = screen.getByPlaceholderText(
      "Search registration or model",
    );
    await user.type(searchInput, "Yutong");

    await waitFor(() => {
      expect(screen.queryByText("WP NB-4821")).not.toBeInTheDocument();
    });
    expect(screen.getByText("WP ND-9012")).toBeInTheDocument();
    expect(searchParamsSeen).toContain("Yutong");
  });

  it("VehiclesPage hides the Register vehicle action when rendered in read-only mode", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
    );

    renderWithProviders(<VehiclesPage centreId={CENTRE_ID} readOnly />, {
      route: `/admin/centres/${CENTRE_ID}/vehicles`,
      user: buildStaff("Admin", CENTRE_ID),
    });

    expect(await screen.findByText("WP NB-4821")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /Register vehicle/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /Back to centre/i }),
    ).toBeInTheDocument();
  });

  it("VehicleProfilePage renders the vehicle details, assigned centre code, and maintenance history from the API", async () => {
    server.use(
      http.get(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`), () =>
        HttpResponse.json(mockVehicleDetail),
      ),
    );

    renderWithProviders(
      <Routes>
        <Route
          path="/fleet/vehicles/:vehicleId"
          element={<VehicleProfilePage />}
        />
      </Routes>,
      {
        route: `/fleet/vehicles/${VEHICLE_ID}`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "WP NB-4821" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Ashok Leyland Viking")).toBeInTheDocument();
    expect(screen.getByText("52 passengers")).toBeInTheDocument();
    expect(screen.getByText("Wheelchair accessible")).toBeInTheDocument();
    expect(screen.getByText("Centre Code: CMB")).toBeInTheDocument();
    expect(screen.getByText(/Brake inspection/)).toBeInTheDocument();
    expect(
      screen.getByText("Front and rear drum lining replacement"),
    ).toBeInTheDocument();
  });

  it("VehicleProfilePage sends DELETE /api/v1/vehicles/:vehicleId when deactivation is confirmed and returns to the vehicle list", async () => {
    let deleteCalledWithId: string | null = null;
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);

    server.use(
      http.get(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`), () =>
        HttpResponse.json(mockVehicleDetail),
      ),
      http.delete(apiUrl("/api/v1/vehicles/:vehicleId"), ({ params }) => {
        deleteCalledWithId = String(params.vehicleId);
        return new HttpResponse(null, { status: 204 });
      }),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () => HttpResponse.json([])),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/vehicles/:vehicleId"
          element={<VehicleProfilePage />}
        />
        <Route path="/fleet/vehicles" element={<VehiclesPage />} />
      </Routes>,
      {
        route: `/fleet/vehicles/${VEHICLE_ID}`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    const deactivateBtn = await screen.findByRole("button", {
      name: "Deactivate",
    });
    await user.click(deactivateBtn);

    expect(confirmSpy).toHaveBeenCalled();
    expect(deleteCalledWithId).toBe(VEHICLE_ID);
    expect(
      await screen.findByRole("heading", { name: "Vehicles" }),
    ).toBeInTheDocument();

    confirmSpy.mockRestore();
  });

  it("VehiclesPage shows a loading indicator while fetching and an empty-state message when the centre has no vehicles", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () => HttpResponse.json([])),
    );

    renderWithProviders(<VehiclesPage />, {
      route: "/fleet/vehicles",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(screen.getByText("Loading vehicles...")).toBeInTheDocument();
    expect(
      await screen.findByText("No vehicles registered for this centre yet."),
    ).toBeInTheDocument();
  });

  it("VehiclesPage shows the error banner when GET /api/v1/vehicles fails with 500 and reloads when Retry is clicked", async () => {
    let attempts = 0;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () => {
        attempts += 1;
        if (attempts === 1) {
          return HttpResponse.json(
            { error: "Database connection failed" },
            { status: 500 },
          );
        }
        return HttpResponse.json(mockVehicles);
      }),
    );

    const { user } = renderWithProviders(<VehiclesPage />, {
      route: "/fleet/vehicles",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    expect(
      await screen.findByText(
        "Failed to load vehicles: Database connection failed",
      ),
    ).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Retry" }));

    expect(await screen.findByText("WP NB-4821")).toBeInTheDocument();
  });

  it("VehicleProfilePage displays a 403 Forbidden error message when a FleetOfficer tries to deactivate another centre's vehicle", async () => {
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);

    server.use(
      http.get(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`), () =>
        HttpResponse.json({
          ...mockVehicleDetail,
          centreId: OTHER_CENTRE_ID,
        }),
      ),
      http.delete(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`), () =>
        HttpResponse.json(
          { error: "You do not have permission to manage this centre." },
          { status: 403 },
        ),
      ),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/vehicles/:vehicleId"
          element={<VehicleProfilePage />}
        />
      </Routes>,
      {
        route: `/fleet/vehicles/${VEHICLE_ID}`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    const deactivateBtn = await screen.findByRole("button", {
      name: "Deactivate",
    });
    await user.click(deactivateBtn);

    expect(
      await screen.findByText(
        "You do not have permission to manage this centre.",
      ),
    ).toBeInTheDocument();

    confirmSpy.mockRestore();
  });
});
