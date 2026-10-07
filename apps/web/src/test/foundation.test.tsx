import { describe, expect, it } from "vitest";
import { useLocation } from "react-router";
import { useAuth } from "@/hooks/useAuth";
import { buildStaff, buildUser } from "./factories";
import { apiUrl, http, HttpResponse, loggedInHandlers } from "./handlers";
import { renderWithProviders, screen, userEvent } from "./render";
import { server } from "./server";

// Self-check of the test foundation. If these fail, the setup (packages, config, mocks) is broken, not the app.

function Probe() {
  const { user } = useAuth();
  const { pathname } = useLocation();
  return (
    <p>
      {pathname} as {user ? `${user.name} (${user.role})` : "anonymous"}
    </p>
  );
}

describe("test foundation", () => {
  it("renders inside the router and auth providers (anonymous by default)", () => {
    renderWithProviders(<Probe />, { route: "/admin/fleet" });

    expect(screen.getByText("/admin/fleet as anonymous")).toBeInTheDocument();
  });

  it("can render as a logged-in user with a given role", () => {
    renderWithProviders(<Probe />, { user: buildStaff("Dispatcher") });

    expect(screen.getByText(/Dispatcher User \(Dispatcher\)/)).toBeInTheDocument();
  });

  it("exposes spy functions for the auth actions", async () => {
    function LogoutButton() {
      const { logout } = useAuth();
      return <button onClick={logout}>Sign out</button>;
    }
    const { user, auth } = renderWithProviders(<LogoutButton />, { user: buildUser() });

    await user.click(screen.getByRole("button", { name: "Sign out" }));

    expect(auth.logout).toHaveBeenCalledTimes(1);
  });

  it("answers API calls from the mock server (anonymous /auth/me is 401 by default)", async () => {
    const response = await fetch(apiUrl("/api/v1/auth/me"));

    expect(response.status).toBe(401);
  });

  it("lets a test override a response, and resets it for the next test", async () => {
    server.use(...loggedInHandlers(buildUser({ name: "Override" })));
    const body = await (await fetch(apiUrl("/api/v1/auth/me"))).json();

    expect(body.name).toBe("Override");
  });

  it("starts the next test with the default handlers again", async () => {
    expect((await fetch(apiUrl("/api/v1/auth/me"))).status).toBe(401);
  });

  it("fails loudly when a request has no mock", async () => {
    await expect(fetch(apiUrl("/api/v1/not-mocked"))).rejects.toThrow();
  });

  it("supports one-off handlers for errors", async () => {
    server.use(http.get(apiUrl("/api/v1/vehicles"), () => HttpResponse.json({ error: "boom" }, { status: 500 })));

    expect((await fetch(apiUrl("/api/v1/vehicles"))).status).toBe(500);
  });

  it("provides jsdom shims used by the UI libraries", () => {
    expect(window.matchMedia("(max-width: 768px)").matches).toBe(false);
    expect(typeof ResizeObserver).toBe("function");
  });

  it("exports user-event for interactions", async () => {
    expect(typeof userEvent.setup).toBe("function");
  });
});
