import { describe, expect, it, vi } from "vitest";
import { PassengerForm } from "@/features/riders/components/PassengerProfile";
import { FareRulesPage } from "@/features/fares/components/FareRules";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

describe("Passenger form", () => {
  it("keeps saving disabled until name and phone are provided, then creates the passenger", async () => {
    let requestBody: unknown;
    const onSaved = vi.fn();
    server.use(
      http.post(apiUrl("/api/v1/passengers"), async ({ request }) => {
        requestBody = await request.json();
        return HttpResponse.json({
          id: "passenger-2",
          fullName: "Mala Silva",
          phoneNumber: "0712345678",
          category: "Student",
          balance: 0,
        });
      }),
    );

    const { user } = renderWithProviders(
      <PassengerForm onSaved={onSaved} onClose={vi.fn()} />,
    );

    const save = screen.getByRole("button", { name: "Save passenger" });
    expect(save).toBeDisabled();
    await user.type(screen.getByLabelText("Full name"), "Mala Silva");
    expect(save).toBeDisabled();
    await user.type(screen.getByLabelText("Phone number"), "0712345678");
    expect(save).toBeEnabled();
    await user.click(save);

    expect(requestBody).toEqual({
      fullName: "Mala Silva",
      phoneNumber: "0712345678",
      email: null,
      category: "Adult",
    });
    expect(onSaved).toHaveBeenCalledWith("passenger-2");
  });
});

describe("Fare rule integration", () => {
  it("sends the selected route, category, and amount when saving a fare rule", async () => {
    let requestBody: unknown;
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
      http.post(apiUrl("/api/v1/fare-rules"), async ({ request }) => {
        requestBody = await request.json();
        return HttpResponse.json({ id: "fare-1" });
      }),
    );

    const { user } = renderWithProviders(<FareRulesPage />, {
      route: "/fares/fare-rules",
      user: buildStaff("Dispatcher", "centre-1"),
    });

    await user.click(screen.getByRole("button", { name: "Create fare rule" }));
    await user.click(screen.getByText("Select an active route"));
    await user.click(await screen.findByRole("option", { name: /101.*Colombo Fort/ }));
    await user.type(screen.getByRole("spinbutton", { name: "Fare (LKR)" }), "125");
    await user.click(screen.getByRole("button", { name: "Save fare rule" }));

    expect(requestBody).toEqual({
      routeId: "route-1",
      amount: 125,
    });
  });
});