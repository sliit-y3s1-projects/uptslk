import { describe, expect, it } from "vitest";
import App from "@/App";
import { RequireRole } from "@/components/auth/RequireAuth";
import { buildStaff, buildUser } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID } from "./fixtures";

const STAFF = ["CentreManager", "Dispatcher", "FleetOfficer", "Driver"];
const page = <div>Operations page</div>;

describe("Dispatch pages: protected routes", () => {
  it.each(STAFF)("lets a %s see the operations pages", (role) => {
    renderWithProviders(<RequireRole role={STAFF}>{page}</RequireRole>, { route: "/operations/dispatch", user: buildStaff(role as never, CENTRE_ID) });

    expect(screen.getByText("Operations page")).toBeInTheDocument();
  });

  it.each(["Commuter", "Admin"])("blocks a %s from the centre staff area with the Access Restricted message", (role) => {
    renderWithProviders(<RequireRole role={STAFF}>{page}</RequireRole>, { route: "/operations/dispatch", user: buildUser({ role }) });

    expect(screen.getByRole("heading", { name: "Access Restricted" })).toBeInTheDocument();
    expect(screen.getByText(/only available to CentreManager, Dispatcher, FleetOfficer, Driver accounts/)).toBeInTheDocument();
    expect(screen.queryByText("Operations page")).not.toBeInTheDocument();
  });

  it("shows nothing to an anonymous visitor", () => {
    renderWithProviders(<RequireRole role={STAFF}>{page}</RequireRole>, { route: "/operations/dispatch", user: null });

    expect(screen.queryByText("Operations page")).not.toBeInTheDocument();
    expect(screen.queryByText("Access Restricted")).not.toBeInTheDocument();
  });

  it("shows the not-found page for an unknown address, even for a signed-in dispatcher", () => {
    renderWithProviders(<RequireRole role={STAFF}>{page}</RequireRole>, { route: "/not-a-real-area", user: buildStaff("Dispatcher", CENTRE_ID) });

    expect(screen.getByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("Operations page")).not.toBeInTheDocument();
  });

  it("lets a dispatcher open the dispatch board in the real app", async () => {
    server.use(http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json([])));

    renderWithProviders(<App />, { route: "/operations/dispatch", user: buildStaff("Dispatcher", CENTRE_ID) });

    expect(await screen.findByRole("heading", { name: "Dispatch control" })).toBeInTheDocument();
  });

  it.each(["/operations/dispatch", "/operations/incidents", "/operations/bays", "/fleet/drivers"])(
    "does not give a commuter %s in the real app",
    async (route) => {
      renderWithProviders(<App />, { route, user: buildUser({ role: "Commuter" }) });

      expect(await screen.findByRole("heading", { name: "Access Restricted" })).toBeInTheDocument();
      await waitFor(() => expect(screen.queryByRole("heading", { name: "Dispatch control" })).not.toBeInTheDocument());
    },
  );
});
