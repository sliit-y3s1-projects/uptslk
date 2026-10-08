import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import {
  VehicleFormPage,
  VehicleProfilePage,
} from "@/features/fleet/VehiclesPage";
import type {
  CentreOption,
  CreateVehicleRequest,
  UpdateVehicleRequest,
  VehicleDetail,
} from "@/features/fleet/types";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { fireEvent, renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";

const CENTRE_ID = "00000000-0000-4000-8000-0000000000c1";
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
  type: "Normal",
  capacity: 52,
  isAccessible: false,
  status: "Active",
  imageUrl: null,
  maintenance: [],
};

beforeEach(() => {
  URL.createObjectURL ??= () => "blob:vehicle-preview";
  URL.revokeObjectURL ??= () => {};
});

describe("VehicleFormPage", () => {
  it("VehicleFormPage shows an error when registration number or model is blank and does not send a POST request", async () => {
    let postCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.post(apiUrl("/api/v1/vehicles"), () => {
        postCalled = true;
        return HttpResponse.json({}, { status: 201 });
      }),
    );

    const { user } = renderWithProviders(<VehicleFormPage />, {
      route: "/fleet/vehicles/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const plateInput = await screen.findByLabelText(/Registration number/i);
    const modelInput = screen.getByLabelText(/Vehicle model/i);

    await user.type(plateInput, "   ");
    await user.type(modelInput, "Ashok Leyland Viking");
    await user.click(screen.getByRole("button", { name: "Register vehicle" }));

    expect(
      await screen.findByText("Registration plate number is required."),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);

    await user.clear(plateInput);
    await user.type(plateInput, "WP NB-4821");
    await user.clear(modelInput);
    await user.type(modelInput, "   ");
    await user.click(screen.getByRole("button", { name: "Register vehicle" }));

    expect(
      await screen.findByText("Vehicle model is required."),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);
  });

  it("VehicleFormPage rejects a zero or negative passenger capacity and does not send a POST request", async () => {
    let postCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.post(apiUrl("/api/v1/vehicles"), () => {
        postCalled = true;
        return HttpResponse.json({}, { status: 201 });
      }),
    );

    const { user } = renderWithProviders(<VehicleFormPage />, {
      route: "/fleet/vehicles/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const plateInput = await screen.findByLabelText(/Registration number/i);
    const modelInput = screen.getByLabelText(/Vehicle model/i);
    const capacityInput = screen.getByLabelText(/Seated passenger capacity/i);

    await user.type(plateInput, "WP NB-4821");
    await user.type(modelInput, "Ashok Leyland Viking");
    await user.clear(capacityInput);
    await user.type(capacityInput, "0");

    const submitButton = screen.getByRole("button", {
      name: "Register vehicle",
    });
    fireEvent.submit(submitButton.closest("form")!);

    expect(
      await screen.findByText("Passenger capacity must be a positive number."),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);
  });

  it("VehicleFormPage rejects a passenger capacity above 200 and does not send a POST request", async () => {
    let postCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.post(apiUrl("/api/v1/vehicles"), () => {
        postCalled = true;
        return HttpResponse.json(
          {
            id: VEHICLE_ID,
            centreId: CENTRE_ID,
            plateNumber: "WP NB-4821",
            model: "Ashok Leyland Viking",
            status: "Active",
          },
          { status: 201 },
        );
      }),
    );

    const { user } = renderWithProviders(<VehicleFormPage />, {
      route: "/fleet/vehicles/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const plateInput = await screen.findByLabelText(/Registration number/i);
    const modelInput = screen.getByLabelText(/Vehicle model/i);
    const capacityInput = screen.getByLabelText(/Seated passenger capacity/i);

    await user.type(plateInput, "WP NB-4821");
    await user.type(modelInput, "Ashok Leyland Viking");
    await user.clear(capacityInput);
    await user.type(capacityInput, "250");

    const submitButton = screen.getByRole("button", {
      name: "Register vehicle",
    });
    fireEvent.submit(submitButton.closest("form")!);

    expect(
      await screen.findByText(/capacity.*between 1 and 200|capacity.*200/i),
    ).toBeInTheDocument();
    expect(postCalled).toBe(false);
  });

  it("VehicleFormPage rejects an unsupported image file type with 'Choose a JPG, PNG, or WebP vehicle image.'", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
    );

    renderWithProviders(<VehicleFormPage />, {
      route: "/fleet/vehicles/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const fileInput = await screen.findByLabelText(
      /Click to upload a vehicle image/i,
    );
    const invalidFile = new File(["%PDF-1.4"], "manual.pdf", {
      type: "application/pdf",
    });

    fireEvent.change(fileInput, { target: { files: [invalidFile] } });

    expect(
      await screen.findByText("Choose a JPG, PNG, or WebP vehicle image."),
    ).toBeInTheDocument();
  });

  it("VehicleFormPage rejects an image file over 5 MB and sends no image upload", async () => {
    let imageUploadCalled = false;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.post(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}/image`), () => {
        imageUploadCalled = true;
        return HttpResponse.json({ imageUrl: "/uploaded.png" });
      }),
    );

    const { user } = renderWithProviders(<VehicleFormPage />, {
      route: "/fleet/vehicles/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const fileInput = await screen.findByLabelText(
      /Click to upload a vehicle image/i,
    );
    const oversizedFile = new File(
      [new Uint8Array(5 * 1024 * 1024 + 1)],
      "huge-bus.png",
      { type: "image/png" },
    );

    await user.upload(fileInput, oversizedFile);

    expect(
      await screen.findByText("The vehicle image must be 5 MB or smaller."),
    ).toBeInTheDocument();
    expect(imageUploadCalled).toBe(false);
  });

  it("VehicleFormPage sends a normalized uppercase plate number in POST /api/v1/vehicles, uploads the selected image, and navigates to the new vehicle profile", async () => {
    let capturedBody: CreateVehicleRequest | null = null;
    let imageUploadedForVehicle: string | null = null;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.post(apiUrl("/api/v1/vehicles"), async ({ request }) => {
        capturedBody = (await request.json()) as CreateVehicleRequest;
        return HttpResponse.json(
          {
            id: VEHICLE_ID,
            centreId: capturedBody.centreId,
            plateNumber: capturedBody.plateNumber,
            model: capturedBody.model,
            status: capturedBody.status,
          },
          { status: 201 },
        );
      }),
      http.post(
        apiUrl("/api/v1/vehicles/:vehicleId/image"),
        ({ params }) => {
          imageUploadedForVehicle = String(params.vehicleId);
          return HttpResponse.json({
            imageUrl: "https://storage.upts.test/bus.png",
          });
        },
      ),
      http.get(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`), () =>
        HttpResponse.json(mockVehicleDetail),
      ),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route path="/fleet/vehicles/new" element={<VehicleFormPage />} />
        <Route
          path="/fleet/vehicles/:vehicleId"
          element={<VehicleProfilePage />}
        />
      </Routes>,
      {
        route: "/fleet/vehicles/new",
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    const plateInput = await screen.findByLabelText(/Registration number/i);
    const modelInput = screen.getByLabelText(/Vehicle model/i);
    const fileInput = screen.getByLabelText(/Click to upload a vehicle image/i);

    await user.type(plateInput, "  wp nb-4821 ");
    await user.type(modelInput, "Ashok Leyland Viking");
    await user.click(
      screen.getByRole("checkbox", { name: /Wheelchair accessible/i }),
    );

    const validImage = new File(["png-bytes"], "bus.png", {
      type: "image/png",
    });
    await user.upload(fileInput, validImage);

    await user.click(screen.getByRole("button", { name: "Register vehicle" }));

    await waitFor(() => {
      expect(capturedBody).not.toBeNull();
    });
    expect(capturedBody).toEqual({
      centreId: CENTRE_ID,
      plateNumber: "WP NB-4821",
      model: "Ashok Leyland Viking",
      type: "Normal",
      capacity: 52,
      isAccessible: true,
      status: "Active",
    });
    expect(imageUploadedForVehicle).toBe(VEHICLE_ID);
    expect(
      await screen.findByRole("heading", { name: "WP NB-4821" }),
    ).toBeInTheDocument();
  });

  it("VehicleFormPage in edit mode loads existing data, sends PUT /api/v1/vehicles/:vehicleId with updated fields, and navigates back to the profile", async () => {
    let capturedUpdate: UpdateVehicleRequest | null = null;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.get(apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`), () =>
        HttpResponse.json(mockVehicleDetail),
      ),
      http.put(
        apiUrl(`/api/v1/vehicles/${VEHICLE_ID}`),
        async ({ request }) => {
          capturedUpdate = (await request.json()) as UpdateVehicleRequest;
          return new HttpResponse(null, { status: 204 });
        },
      ),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route
          path="/fleet/vehicles/:vehicleId/edit"
          element={<VehicleFormPage />}
        />
        <Route
          path="/fleet/vehicles/:vehicleId"
          element={<VehicleProfilePage />}
        />
      </Routes>,
      {
        route: `/fleet/vehicles/${VEHICLE_ID}/edit`,
        user: buildStaff("FleetOfficer", CENTRE_ID),
      },
    );

    expect(
      await screen.findByRole("heading", { name: "Edit WP NB-4821" }),
    ).toBeInTheDocument();

    const modelInput = screen.getByLabelText(/Vehicle model/i);
    const capacityInput = screen.getByLabelText(/Seated passenger capacity/i);

    await user.clear(modelInput);
    await user.type(modelInput, "Tata LP 909");
    await user.clear(capacityInput);
    await user.type(capacityInput, "44");

    await user.click(screen.getByRole("button", { name: "Save changes" }));

    await waitFor(() => {
      expect(capturedUpdate).not.toBeNull();
    });
    expect(capturedUpdate).toEqual({
      centreId: CENTRE_ID,
      plateNumber: "WP NB-4821",
      model: "Tata LP 909",
      type: "Normal",
      capacity: 44,
      isAccessible: false,
      status: "Active",
    });
  });

  it("VehicleFormPage displays the 409 Conflict message (A vehicle with this plate number already exists.) and disables the submit button while saving", async () => {
    let resolvePost!: (response: Response) => void;

    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(mockCentres)),
      http.post(
        apiUrl("/api/v1/vehicles"),
        () =>
          new Promise<Response>((resolve) => {
            resolvePost = resolve;
          }),
      ),
    );

    const { user } = renderWithProviders(<VehicleFormPage />, {
      route: "/fleet/vehicles/new",
      user: buildStaff("FleetOfficer", CENTRE_ID),
    });

    const plateInput = await screen.findByLabelText(/Registration number/i);
    await user.type(plateInput, "WP NB-4821");
    await user.type(
      screen.getByLabelText(/Vehicle model/i),
      "Ashok Leyland Viking",
    );

    await user.click(screen.getByRole("button", { name: "Register vehicle" }));

    expect(
      await screen.findByRole("button", { name: /Registering.../i }),
    ).toBeDisabled();

    resolvePost(
      HttpResponse.json(
        { error: "A vehicle with this plate number already exists." },
        { status: 409 },
      ),
    );

    expect(
      await screen.findByText(
        "A vehicle with this plate number already exists.",
      ),
    ).toBeInTheDocument();
  });
});

