import { describe, expect, it } from "vitest";
import { Route, Routes } from "react-router";
import App from "@/App";
import { RequireRole } from "@/components/auth/RequireAuth";
import { CentreFormPage, CentreProfilePage } from "@/features/centres/CentresPage";
import { buildStaff, buildUser } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";

const centreDetail = (overrides: Record<string, unknown> = {}) => ({
  id: "centre-1", code: "kan", name: "Kandy Central", city: "Kandy", district: "Kandy", description: "Main hill-country hub",
  status: "Operating",
  bays: [
    { id: "bay-1", code: "B1", name: "Bay 1", status: "Available" },
    { id: "bay-2", code: "B2", name: "Bay 2", status: "OutOfService" },
  ],
  routes: [],
  ...overrides,
});

const employees = [
  { id: "u-1", name: "Kamal Manager", role: "CentreManager", centreId: "centre-1" },
  { id: "u-2", name: "Dina Dispatcher", role: "Dispatcher", centreId: "centre-1" },
  { id: "u-3", name: "Someone Else", role: "Dispatcher", centreId: "centre-9" },
];

function mockCentre(detail = centreDetail()) {
  server.use(
    http.get(apiUrl("/api/v1/centres/centre-1"), () => HttpResponse.json(detail)),
    http.get(apiUrl("/api/v1/auth/users"), () => HttpResponse.json(employees)),
  );
}

function renderProfile() {
  return renderWithProviders(
    <Routes>
      <Route path="/admin/centres/:centreId" element={<CentreProfilePage />} />
    </Routes>,
    { route: "/admin/centres/centre-1", user: buildStaff("Admin") },
  );
}

describe("CentreProfilePage", () => {
  it("shows the centre details, bay count, manager and employee count", async () => {
    mockCentre();

    renderProfile();

    expect(await screen.findByRole("heading", { name: "Kandy Central" })).toBeInTheDocument();
    expect(screen.getByText("KAN")).toBeInTheDocument(); // the code is shown in upper case
    expect(screen.getByText("Main hill-country hub")).toBeInTheDocument();
    expect(screen.getByText("Bus bays").nextSibling).toHaveTextContent("2");
    expect(await screen.findByText("Kamal Manager")).toBeInTheDocument();
    expect(screen.getByText("Employees").nextSibling).toHaveTextContent("2"); // only this centre's staff
  });

  it("offers View operations only for an operating centre", async () => {
    mockCentre(centreDetail({ status: "Planned" }));

    renderProfile();

    expect(await screen.findByRole("button", { name: /Edit centre/ })).toHaveAttribute("href", "/admin/centres/centre-1/edit");
    expect(screen.queryByRole("button", { name: /View operations/ })).not.toBeInTheDocument();
  });

  it("links an operating centre to its operations", async () => {
    mockCentre();

    renderProfile();

    expect(await screen.findByRole("button", { name: /View operations/ })).toHaveAttribute("href", "/admin/centres/centre-1/operations");
  });

  it("shows 'Unassigned' when the centre has no manager and a message when the centre cannot be loaded", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres/centre-1"), () => HttpResponse.json(centreDetail())),
      http.get(apiUrl("/api/v1/auth/users"), () => HttpResponse.json([])),
    );
    const view = renderProfile();
    expect(await screen.findByText("Unassigned")).toBeInTheDocument();
    view.unmount();

    server.use(http.get(apiUrl("/api/v1/centres/centre-1"), () => HttpResponse.json({ error: "x" }, { status: 500 })));
    renderProfile();
    expect(await screen.findByText("Failed to load centre.")).toBeInTheDocument();
  });
});

describe("CentreFormPage in edit mode", () => {
  function renderEdit() {
    return renderWithProviders(
      <Routes>
        <Route path="/admin/centres/:centreId/edit" element={<CentreFormPage />} />
        <Route path="/admin/centres/:centreId" element={<div>Profile page</div>} />
      </Routes>,
      { route: "/admin/centres/centre-1/edit", user: buildStaff("Admin") },
    );
  }

  it("loads the existing centre into the form and locks the centre code", async () => {
    mockCentre();

    renderEdit();

    expect(await screen.findByRole("heading", { name: "Edit Kandy Central" })).toBeInTheDocument();
    expect(screen.getByLabelText(/Centre name/i)).toHaveValue("Kandy Central");
    expect(screen.getByLabelText(/Centre code/i)).toBeDisabled();
  });

  it("sends PUT /api/v1/centres/{id} with the changed name and returns to the profile", async () => {
    mockCentre();
    let body: Record<string, unknown> = {};
    server.use(
      http.put(apiUrl("/api/v1/centres/centre-1"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { user } = renderEdit();
    const name = await screen.findByLabelText(/Centre name/i);

    await user.clear(name);
    await user.type(name, "Kandy Hill Hub");
    await user.click(screen.getByRole("button", { name: /Save|Update/i }));

    expect(await screen.findByText("Profile page")).toBeInTheDocument();
    expect(body).toMatchObject({ name: "Kandy Hill Hub", city: "Kandy", district: "Kandy", status: "Operating" });
  });

  it("shows a message and stays on the form when saving fails", async () => {
    mockCentre();
    server.use(http.put(apiUrl("/api/v1/centres/centre-1"), () => HttpResponse.json({ error: "x" }, { status: 500 })));
    const { user } = renderEdit();
    await screen.findByRole("heading", { name: "Edit Kandy Central" });

    await user.click(screen.getByRole("button", { name: /Save|Update/i }));

    expect(await screen.findByText("Failed to save centre. Please try again.")).toBeInTheDocument();
    expect(screen.queryByText("Profile page")).not.toBeInTheDocument();
  });

  it("shows a message when the centre to edit cannot be loaded", async () => {
    server.use(http.get(apiUrl("/api/v1/centres/centre-1"), () => HttpResponse.json({ error: "x" }, { status: 500 })));

    renderEdit();

    expect(await screen.findByText("Failed to load centre for editing.")).toBeInTheDocument();
  });
});

describe("Centre pages: protected routes", () => {
  const page = <div>Centres admin page</div>;

  it("shows the page to an Admin", () => {
    renderWithProviders(<RequireRole role="Admin">{page}</RequireRole>, { route: "/admin/centres", user: buildUser({ role: "Admin" }) });

    expect(screen.getByText("Centres admin page")).toBeInTheDocument();
  });

  it.each(["CentreManager", "Dispatcher", "FleetOfficer", "Driver", "Commuter"])("blocks a %s with the Access Restricted message", (role) => {
    renderWithProviders(<RequireRole role="Admin">{page}</RequireRole>, { route: "/admin/centres", user: buildUser({ role }) });

    expect(screen.getByRole("heading", { name: "Access Restricted" })).toBeInTheDocument();
    expect(screen.queryByText("Centres admin page")).not.toBeInTheDocument();
  });

  it("shows nothing to an anonymous visitor", () => {
    renderWithProviders(<RequireRole role="Admin">{page}</RequireRole>, { route: "/admin/centres", user: null });

    expect(screen.queryByText("Centres admin page")).not.toBeInTheDocument();
    expect(screen.queryByText("Access Restricted")).not.toBeInTheDocument();
  });

  it("lets an Admin open /admin/centres in the real app", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/auth/users"), () => HttpResponse.json([])),
    );

    renderWithProviders(<App />, { route: "/admin/centres", user: buildUser({ role: "Admin" }) });

    expect(await screen.findByRole("heading", { name: "Multimodal centres" })).toBeInTheDocument();
  });

  it("does not give a commuter the centres admin page in the real app", async () => {
    renderWithProviders(<App />, { route: "/admin/centres", user: buildUser({ role: "Commuter" }) });

    expect(await screen.findByRole("heading", { name: "Access Restricted" })).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole("heading", { name: "Multimodal centres" })).not.toBeInTheDocument());
  });
});
