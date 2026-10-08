import { Route, Routes } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { DriverDetailPage, DriverFormPage, DriversPage } from "@/features/fleet/DriversPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID } from "./fixtures";

const manager = buildStaff("CentreManager", CENTRE_ID);
const centres = [{ id: CENTRE_ID, code: "CMB", name: "Colombo Fort", city: "Colombo", district: "Colombo", status: "Operating", bayCount: 4, routeCount: 2 }];
const driver = (id: string, fullName: string, extra: Record<string, unknown> = {}) => ({
  id, centreId: CENTRE_ID, centre: "Colombo Fort", fullName, licenseNumber: `DL-${id}`, phoneNumber: "0771234567", status: "Active", ...extra,
});
const detail = (extra: Record<string, unknown> = {}) => ({
  ...driver("1", "Kamal Perera"), email: "kamal@upts.lk", centre: { id: CENTRE_ID, code: "CMB", name: "Colombo Fort" }, userId: "u-1", ...extra,
});

function mockLookups(extra: Parameters<typeof server.use> = []) {
  server.use(...extra, http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json(centres)));
}

describe("DriversPage", () => {
  it("lists the centre's drivers with licence, phone and status", async () => {
    mockLookups([http.get(apiUrl("/api/v1/drivers"), () => HttpResponse.json([driver("1", "Kamal Perera"), driver("2", "Nimal Silva", { status: "Inactive", phoneNumber: null })]))]);

    renderWithProviders(<DriversPage />, { route: "/fleet/drivers", user: manager });

    expect(await screen.findByText("Kamal Perera")).toBeInTheDocument();
    expect(screen.getByText(/DL-1 · 0771234567/)).toBeInTheDocument();
    expect(screen.getByText(/DL-2 · No phone recorded/)).toBeInTheDocument();
    expect(screen.getByText("Inactive")).toBeInTheDocument();
    expect(screen.getByText(/2 drivers assigned to Colombo Fort/)).toBeInTheDocument();
  });

  it("asks the API to search when the dispatcher types a name or licence", async () => {
    const searches: (string | null)[] = [];
    mockLookups([
      http.get(apiUrl("/api/v1/drivers"), ({ request }) => {
        searches.push(new URL(request.url).searchParams.get("search"));
        return HttpResponse.json([]);
      }),
    ]);
    const { user: person } = renderWithProviders(<DriversPage />, { route: "/fleet/drivers", user: manager });
    await screen.findByText("No drivers registered for this centre yet.");

    await person.type(screen.getByPlaceholderText("Search name or licence"), "kamal");

    expect(await screen.findByText("No drivers match this search.")).toBeInTheDocument();
    expect(searches).toContain("kamal");
  });

  it("shows a loading message and then an empty message", async () => {
    mockLookups([http.get(apiUrl("/api/v1/drivers"), () => HttpResponse.json([]))]);

    renderWithProviders(<DriversPage />, { route: "/fleet/drivers", user: manager });

    expect(screen.getByText(/Loading drivers/)).toBeInTheDocument();
    expect(await screen.findByText("No drivers registered for this centre yet.")).toBeInTheDocument();
  });

  it("shows the error and loads the drivers again when Retry is clicked", async () => {
    // The page asks for drivers more than once while the centre is resolved, so the outage is a flag, not a call count.
    let outage = true;
    mockLookups([
      http.get(apiUrl("/api/v1/drivers"), () =>
        outage ? HttpResponse.json({ error: "Database unavailable" }, { status: 503 }) : HttpResponse.json([driver("1", "Kamal Perera")]),
      ),
    ]);
    const { user: person } = renderWithProviders(<DriversPage />, { route: "/fleet/drivers", user: manager });

    expect(await screen.findByText(/Failed to load drivers: Database unavailable/)).toBeInTheDocument();
    outage = false;
    await person.click(screen.getByRole("button", { name: "Retry" }));

    expect(await screen.findByText("Kamal Perera")).toBeInTheDocument();
  });
});

describe("DriverFormPage", () => {
  const renderForm = () =>
    renderWithProviders(
      <Routes>
        <Route path="/fleet/drivers/new" element={<DriverFormPage />} />
        <Route path="/fleet/drivers/:driverId" element={<div>Driver profile page</div>} />
      </Routes>,
      { route: "/fleet/drivers/new", user: manager },
    );

  const fill = async (person: ReturnType<typeof renderForm>["user"], values: { name?: string; email?: string; password?: string; licence?: string }) => {
    if (values.name) await person.type(screen.getByPlaceholderText("Driver full name"), values.name);
    if (values.email) await person.type(screen.getByPlaceholderText("driver@upts.lk"), values.email);
    if (values.password) await person.type(screen.getByPlaceholderText("At least 8 characters"), values.password);
    if (values.licence) await person.type(screen.getByPlaceholderText("B-457829"), values.licence);
  };

  it.each([
    ["no licence number", { name: "Kamal", email: "k@upts.lk", password: "Passw0rd!" }, "Licence number is required."],
    ["no email", { name: "Kamal", licence: "dl-1", password: "Passw0rd!" }, "A sign-in email is required."],
    ["a short password", { name: "Kamal", email: "k@upts.lk", licence: "dl-1", password: "short" }, "The temporary password must contain at least 8 characters."],
  ])("rejects a driver with %s", async (_label, values, message) => {
    mockLookups();
    let posted = false;
    server.use(http.post(apiUrl("/api/v1/drivers"), () => { posted = true; return HttpResponse.json({ id: "x" }); }));
    const { user: person } = renderForm();
    await screen.findByRole("heading", { name: "Add driver" });
    await fillWithoutNativeBlocking(person, values);

    await person.click(screen.getByRole("button", { name: "Create driver" }));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(posted).toBe(false);
  });

  async function fillWithoutNativeBlocking(person: ReturnType<typeof renderForm>["user"], values: { name?: string; email?: string; password?: string; licence?: string }) {
    // The inputs also have native `required`/`minLength`; submit the form directly so the page's own messages are exercised.
    await fill(person, values);
    screen.getByRole("button", { name: "Create driver" }).closest("form")!.noValidate = true;
  }

  it("sends POST /api/v1/drivers with a lower-case email and upper-case licence, then opens the profile", async () => {
    mockLookups();
    let body: Record<string, unknown> = {};
    server.use(
      http.post(apiUrl("/api/v1/drivers"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ id: "new-driver" }, { status: 201 });
      }),
    );
    const { user: person } = renderForm();
    await screen.findByRole("heading", { name: "Add driver" });

    await fill(person, { name: " Kamal Perera ", email: "Kamal.P@UPTS.lk", password: "Passw0rd!", licence: "dl-100" });
    await person.click(screen.getByRole("button", { name: "Create driver" }));

    expect(await screen.findByText("Driver profile page")).toBeInTheDocument();
    expect(body).toMatchObject({ centreId: CENTRE_ID, fullName: "Kamal Perera", email: "kamal.p@upts.lk", licenseNumber: "DL-100", status: "Active" });
  });

  it("shows the API message when the licence or email already exists", async () => {
    mockLookups();
    server.use(http.post(apiUrl("/api/v1/drivers"), () => HttpResponse.json({ error: "A driver with this license number already exists." }, { status: 409 })));
    const { user: person } = renderForm();
    await screen.findByRole("heading", { name: "Add driver" });

    await fill(person, { name: "Kamal Perera", email: "k@upts.lk", password: "Passw0rd!", licence: "dl-100" });
    await person.click(screen.getByRole("button", { name: "Create driver" }));

    expect(await screen.findByText("A driver with this license number already exists.")).toBeInTheDocument();
    expect(screen.queryByText("Driver profile page")).not.toBeInTheDocument();
  });
});

describe("DriverDetailPage", () => {
  const renderDetail = () =>
    renderWithProviders(
      <Routes>
        <Route path="/fleet/drivers/:driverId" element={<DriverDetailPage />} />
        <Route path="/fleet/drivers" element={<div>Drivers list page</div>} />
      </Routes>,
      { route: "/fleet/drivers/1", user: manager },
    );

  it("shows the driver profile", async () => {
    server.use(http.get(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json(detail())));

    renderDetail();

    expect(await screen.findByRole("heading", { name: "Kamal Perera" })).toBeInTheDocument();
    expect(screen.getByText("kamal@upts.lk")).toBeInTheDocument();
    expect(screen.getByText("DL-1")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Edit driver/ })).toHaveAttribute("href", "/fleet/drivers/1/edit");
  });

  it("deactivates the driver with DELETE /api/v1/drivers/{id} after confirmation and returns to the list", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);
    let deleted = false;
    server.use(
      http.get(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json(detail())),
      http.delete(apiUrl("/api/v1/drivers/1"), () => { deleted = true; return new HttpResponse(null, { status: 204 }); }),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: "Deactivate" }));

    expect(await screen.findByText("Drivers list page")).toBeInTheDocument();
    expect(deleted).toBe(true);
  });

  it("does nothing when the dispatcher declines the confirmation", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(false);
    let deleted = false;
    server.use(
      http.get(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json(detail())),
      http.delete(apiUrl("/api/v1/drivers/1"), () => { deleted = true; return new HttpResponse(null, { status: 204 }); }),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: "Deactivate" }));

    await waitFor(() => expect(screen.getByRole("heading", { name: "Kamal Perera" })).toBeInTheDocument());
    expect(deleted).toBe(false);
  });

  it("hides Deactivate for an inactive driver and shows an error for an unknown driver", async () => {
    server.use(http.get(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json(detail({ status: "Inactive" }))));
    const view = renderDetail();
    await screen.findByRole("heading", { name: "Kamal Perera" });
    expect(screen.queryByRole("button", { name: "Deactivate" })).not.toBeInTheDocument();
    view.unmount();

    server.use(http.get(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json({ error: "Not found" }, { status: 404 })));
    renderDetail();
    expect(await screen.findByText("Not found")).toBeInTheDocument();
  });

  it("shows the API message when deactivation is refused", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);
    server.use(
      http.get(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json(detail())),
      http.delete(apiUrl("/api/v1/drivers/1"), () => HttpResponse.json({ error: "Not allowed for another centre's driver." }, { status: 403 })),
    );
    const { user: person } = renderDetail();

    await person.click(await screen.findByRole("button", { name: "Deactivate" }));

    expect(await screen.findByText("Not allowed for another centre's driver.")).toBeInTheDocument();
  });
});
