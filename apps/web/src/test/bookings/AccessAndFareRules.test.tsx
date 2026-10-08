import { describe, expect, it, vi } from "vitest";
import { RequireRole } from "@/components/auth/RequireAuth";
import { FareRules } from "@/features/fares/components/FareRules";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

describe("RequireRole", () => {
  it("shows the restricted message for a commuter user", () => {
    renderWithProviders(
      <RequireRole role="Dispatcher">
        <div>Dispatcher page</div>
      </RequireRole>,
      { user: buildStaff("Commuter") },
    );

    expect(screen.getByRole("heading", { name: "Access Restricted" })).toBeInTheDocument();
    expect(screen.queryByText("Dispatcher page")).not.toBeInTheDocument();
  });

  it("renders the protected content for an authorised staff role", () => {
    renderWithProviders(
      <RequireRole role="Dispatcher">
        <div>Dispatcher page</div>
      </RequireRole>,
      { user: buildStaff("Dispatcher") },
    );

    expect(screen.getByText("Dispatcher page")).toBeInTheDocument();
  });

  it("shows nothing for an anonymous user", () => {
    renderWithProviders(
      <RequireRole role="Dispatcher">
        <div>Dispatcher page</div>
      </RequireRole>,
      { user: null },
    );

    expect(screen.queryByText("Dispatcher page")).not.toBeInTheDocument();
    expect(screen.queryByText("Access Restricted")).not.toBeInTheDocument();
  });
});

describe("FareRules", () => {
  function mockFareRuleRequests(postSpy: () => void) {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () =>
        HttpResponse.json([{ id: "centre-1", name: "Colombo", code: "CMB" }]),
      ),
      http.get(apiUrl("/api/v1/routes"), () =>
        HttpResponse.json([
          { id: "route-1", routeNumber: "101", name: "Colombo Fort", isActive: true },
        ]),
      ),
      http.get(apiUrl("/api/v1/fare-rules"), () => HttpResponse.json([])),
      http.post(apiUrl("/api/v1/fare-rules"), () => {
        postSpy();
        return HttpResponse.json({ id: "rule-1" });
      }),
    );
  }

  it("rejects a fare rule without a selected route", async () => {
    const postSpy = vi.fn<() => void>();
    mockFareRuleRequests(postSpy);

    const { user } = renderWithProviders(<FareRules />, {
      user: buildStaff("Dispatcher", "centre-1"),
    });

    expect(screen.getByRole("button", { name: "Create fare rule" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Create fare rule" }));
    await user.type(screen.getByRole("spinbutton", { name: "Fare (LKR)" }), "10");
    await user.click(screen.getByRole("button", { name: "Save fare rule" }));

    expect(postSpy).not.toHaveBeenCalled();
    expect(await screen.findByText("Select an active route.")).toBeInTheDocument();
  });

  it("rejects a fare rule with a non-positive amount", async () => {
    const postSpy = vi.fn<() => void>();
    mockFareRuleRequests(postSpy);

    const { user } = renderWithProviders(<FareRules />, {
      user: buildStaff("Dispatcher", "centre-1"),
    });

    await user.click(screen.getByRole("button", { name: "Create fare rule" }));

    const amountInput = screen.getByRole("spinbutton", { name: "Fare (LKR)" });
    await user.clear(amountInput);
    await user.type(amountInput, "0");
    await user.click(screen.getByRole("button", { name: "Save fare rule" }));

    expect(postSpy).not.toHaveBeenCalled();
    expect(await screen.findByText("Fare amount must be greater than zero.")).toBeInTheDocument();
  });
});
