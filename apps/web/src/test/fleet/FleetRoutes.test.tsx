import { describe, expect, it } from "vitest";
import { RequireRole } from "@/components/auth/RequireAuth";
import { MaintenancePage } from "@/features/fleet/MaintenancePage";
import { VehiclesPage } from "@/features/fleet/VehiclesPage";
import type { CentreOption } from "@/features/fleet/types";
import { buildStaff, buildUser } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

const CENTRE_ID = "00000000-0000-4000-8000-0000000000c1";
const FLEET_ROLES = ["Admin", "CentreManager", "FleetOfficer"];

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

describe("Fleet protected routes (RequireRole)", () => {
  it("RequireRole allows FleetOfficer, CentreManager, and Admin to view VehiclesPage and MaintenancePage", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/maintenance-records"), () =>
        HttpResponse.json([]),
      ),
    );

    const { unmount: unmountOfficer } = renderWithProviders(
      <RequireRole role={FLEET_ROLES}>
        <VehiclesPage />
      </RequireRole>,
      {
        route: "/fleet/vehicles",
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Vehicles" }),
    ).toBeInTheDocument();
    unmountOfficer();

    const { unmount: unmountManager } = renderWithProviders(
      <RequireRole role={FLEET_ROLES}>
        <MaintenancePage />
      </RequireRole>,
      {
        route: "/fleet/maintenance",
        user: buildStaff("CentreManager", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Maintenance" }),
    ).toBeInTheDocument();
    unmountManager();

    renderWithProviders(
      <RequireRole role={FLEET_ROLES}>
        <VehiclesPage />
      </RequireRole>,
      {
        route: "/fleet/vehicles",
        user: buildUser({ role: "Admin" }),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Vehicles" }),
    ).toBeInTheDocument();
  });

  it("RequireRole blocks a Commuter and a Driver from /fleet/vehicles and /fleet/maintenance with the Access Restricted message", () => {
    const { unmount } = renderWithProviders(
      <RequireRole role={FLEET_ROLES}>
        <VehiclesPage />
      </RequireRole>,
      {
        route: "/fleet/vehicles",
        user: buildUser({ role: "Commuter" }),
      },
    );

    expect(
      screen.getByRole("heading", { name: "Access Restricted" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "This area is only available to Admin, CentreManager, FleetOfficer accounts.",
      ),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "Vehicles" }),
    ).not.toBeInTheDocument();

    unmount();

    renderWithProviders(
      <RequireRole role={FLEET_ROLES}>
        <MaintenancePage />
      </RequireRole>,
      {
        route: "/fleet/maintenance",
        user: buildStaff("Driver", CENTRE_ID),
      },
    );

    expect(
      screen.getByRole("heading", { name: "Access Restricted" }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("heading", { name: "Maintenance" }),
    ).not.toBeInTheDocument();
  });

  it("RequireRole renders nothing for an anonymous visitor (user: null) on /fleet/vehicles", () => {
    const { container } = renderWithProviders(
      <RequireRole role={FLEET_ROLES}>
        <VehiclesPage />
      </RequireRole>,
      {
        route: "/fleet/vehicles",
        user: null,
      },
    );

    expect(container).toBeEmptyDOMElement();
    expect(
      screen.queryByRole("heading", { name: "Vehicles" }),
    ).not.toBeInTheDocument();
  });
});

