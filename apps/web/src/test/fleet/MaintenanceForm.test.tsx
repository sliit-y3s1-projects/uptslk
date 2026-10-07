import { Route, Routes } from "react-router";
import { describe, expect, it } from "vitest";
import {
  MaintenanceDetailPage,
  MaintenanceFormPage,
} from "@/features/fleet/MaintenancePage";
import type {
  CentreOption,
  CreateMaintenanceRequest,
  MaintenanceDetail,
  UpdateMaintenanceRequest,
  VehicleListItem,
} from "@/features/fleet/types";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { fireEvent, renderWithProviders, screen, waitFor } from "@/test/render";
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

describe("MaintenanceFormPage", () => {
  it("MaintenanceFormPage shows 'Please select a vehicle.' when submitted without a selected vehicle and sends no POST request", async () => {
    let postCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () => HttpResponse.json([])),
      http.post(apiUrl("/api/v1/maintenance-records"), () => {
        postCalled = true;
        return HttpResponse.json({}, { status: 201 });
      }),
    );

    const { user } = renderWithProviders(<MaintenanceFormPage />, {
      route: "/fleet/maintenance/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const typeInput = await screen.findByLabelText(/Maintenance type/i);
    await user.type(typeInput, "Brake inspection");
    await user.type(
      screen.getByLabelText(/Description/i),
      "Inspect brake pads and drums",
    );

    await user.click(
      screen.getByRole("button", { name: "Schedule maintenance" }),
    );

    expect(
      await screen.findByText("Please select a vehicle."),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);
  });

  it("MaintenanceFormPage shows validation errors when maintenance type or description is whitespace and sends no POST request", async () => {
    let postCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
      http.post(apiUrl("/api/v1/maintenance-records"), () => {
        postCalled = true;
        return HttpResponse.json({}, { status: 201 });
      }),
    );

    const { user } = renderWithProviders(<MaintenanceFormPage />, {
      route: "/fleet/maintenance/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const typeInput = await screen.findByLabelText(/Maintenance type/i);
    const descriptionInput = screen.getByLabelText(/Description/i);

    await user.type(typeInput, "   ");
    await user.type(descriptionInput, "Valid description");
    await user.click(
      screen.getByRole("button", { name: "Schedule maintenance" }),
    );

    expect(
      await screen.findByText("Maintenance type is required."),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);

    await user.clear(typeInput);
    await user.type(typeInput, "Routine service");
    await user.clear(descriptionInput);
    await user.type(descriptionInput, "   ");
    await user.click(
      screen.getByRole("button", { name: "Schedule maintenance" }),
    );

    expect(
      await screen.findByText("Description is required."),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);
  });

  it("MaintenanceFormPage in edit mode requires a completion time when status is set to Completed and sends no PUT request", async () => {
    let putCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
      http.get(apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`), () =>
        HttpResponse.json(mockRecordDetail),
      ),
      http.put(apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`), () => {
        putCalled = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/maintenance/:recordId/edit"
          element={<MaintenanceFormPage />}
        />
      </Routes>,
      {
        route: `/fleet/maintenance/${RECORD_ID}/edit`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Edit Engine oil service" }),
    ).toBeInTheDocument();

    const statusTrigger = screen.getByRole("combobox", {
      name: /Maintenance status/i,
    });
    await user.click(statusTrigger);
    await user.click(await screen.findByRole("option", { name: "Completed" }));

    const saveBtn = screen.getByRole("button", { name: "Save changes" });
    fireEvent.submit(saveBtn.closest("form")!);

    expect(
      await screen.findByText(
        "Completed maintenance requires a completion time.",
      ),
    ).toBeInTheDocument();
    expect(putCalled).toBe(false);
  });

  it("MaintenanceFormPage sends POST /api/v1/maintenance-records with the selected vehicle, type, description, and ISO timestamp, then navigates to the detail page", async () => {
    let capturedBody: CreateMaintenanceRequest | null = null;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
      http.post(apiUrl("/api/v1/maintenance-records"), async ({ request }) => {
        capturedBody = (await request.json()) as CreateMaintenanceRequest;
        return HttpResponse.json(
          {
            id: RECORD_ID,
            vehicleId: capturedBody.vehicleId,
            type: capturedBody.type,
            status: "Scheduled",
            scheduledFor: capturedBody.scheduledFor,
          },
          { status: 201 },
        );
      }),
      http.get(apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`), () =>
        HttpResponse.json(mockRecordDetail),
      ),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/maintenance/new"
          element={<MaintenanceFormPage />}
        />
        <Route
          path="/fleet/maintenance/:recordId"
          element={<MaintenanceDetailPage />}
        />
      </Routes>,
      {
        route: "/fleet/maintenance/new",
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    const typeInput = await screen.findByLabelText(/Maintenance type/i);
    await user.type(typeInput, "Engine oil service");
    await user.type(
      screen.getByLabelText(/Description/i),
      "Replace engine oil and primary filter",
    );

    await user.click(
      screen.getByRole("button", { name: "Schedule maintenance" }),
    );

    await waitFor(() => {
      expect(capturedBody).not.toBeNull();
    });
    expect(capturedBody).toMatchObject({
      vehicleId: VEHICLE_ID,
      type: "Engine oil service",
      description: "Replace engine oil and primary filter",
    });
    expect(
      await screen.findByText("Not completed"),
    ).toBeInTheDocument();
  });

  it("MaintenanceFormPage in edit mode sends PUT /api/v1/maintenance-records/:recordId with Completed status and completedAt timestamp", async () => {
    let capturedUpdate: UpdateMaintenanceRequest | null = null;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
      http.get(apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`), () =>
        HttpResponse.json(mockRecordDetail),
      ),
      http.put(
        apiUrl(`/api/v1/maintenance-records/${RECORD_ID}`),
        async ({ request }) => {
          capturedUpdate = (await request.json()) as UpdateMaintenanceRequest;
          return new HttpResponse(null, { status: 204 });
        },
      ),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/maintenance/:recordId/edit"
          element={<MaintenanceFormPage />}
        />
        <Route
          path="/fleet/maintenance/:recordId"
          element={<MaintenanceDetailPage />}
        />
      </Routes>,
      {
        route: `/fleet/maintenance/${RECORD_ID}/edit`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Edit Engine oil service" }),
    ).toBeInTheDocument();

    const statusTrigger = screen.getByRole("combobox", {
      name: /Maintenance status/i,
    });
    await user.click(statusTrigger);
    await user.click(await screen.findByRole("option", { name: "Completed" }));

    const completedAtInput = screen.getByLabelText(/Completed at/i);
    fireEvent.change(completedAtInput, {
      target: { value: "2026-10-20T12:00" },
    });

    await user.click(screen.getByRole("button", { name: "Save changes" }));

    await waitFor(() => {
      expect(capturedUpdate).not.toBeNull();
    });
    expect(capturedUpdate).toMatchObject({
      type: "Engine oil service",
      description: "Replace engine oil and primary filter",
      status: "Completed",
    });
    expect(capturedUpdate!.completedAt).toBeTruthy();
  });

  it("MaintenanceFormPage displays the 409 Conflict message when POST /api/v1/maintenance-records returns a trip clash with conflictingTripIds and disables buttons while submitting", async () => {
    let resolvePost!: (response: Response) => void;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl("/api/v1/vehicles"), () =>
        HttpResponse.json(mockVehicles),
      ),
      http.post(
        apiUrl("/api/v1/maintenance-records"),
        () =>
          new Promise<Response>((resolve) => {
            resolvePost = resolve;
          }),
      ),
    );

    const { user } = renderWithProviders(<MaintenanceFormPage />, {
      route: "/fleet/maintenance/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const typeInput = await screen.findByLabelText(/Maintenance type/i);
    await user.type(typeInput, "Brake service");
    await user.type(
      screen.getByLabelText(/Description/i),
      "Inspect brake system",
    );

    await user.click(
      screen.getByRole("button", { name: "Schedule maintenance" }),
    );

    expect(
      await screen.findByRole("button", { name: "Scheduling..." }),
    ).toBeDisabled();

    resolvePost(
      HttpResponse.json(
        {
          error:
            "Reassign or cancel the vehicle's active trips on this maintenance day before scheduling it.",
          conflictingTripIds: ["00000000-0000-4000-8000-000000000099"],
        },
        { status: 409 },
      ),
    );

    expect(
      await screen.findByText(
        "Reassign or cancel the vehicle's active trips on this maintenance day before scheduling it.",
      ),
    ).toBeInTheDocument();
  });
});

