import { screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test/render";
import { server } from "@/test/server";
import { apiUrl } from "@/test/handlers";
import { buildStaff } from "@/test/factories";
import { RoutesPage, RouteFormPage, RouteDetailPage } from "@/features/network/RoutesPage";

describe("RoutesPage", () => {
  it("displays the route numbers and names from the API", async () => {
    server.use(
      http.get(apiUrl("/api/v1/routes"), () => {
        return HttpResponse.json([
          {
            id: "route-123",
            routeNumber: "138",
            name: "Maharagama - Fort",
            origin: "Maharagama",
            destination: "Fort",
            isActive: true,
          },
        ]);
      }),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres/:id"), () => HttpResponse.json({ name: "Test Centre" }))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/routes" element={<RoutesPage />} />
      </Routes>,
      { route: "/network/routes", user: buildStaff("Admin") }
    );

    expect(await screen.findByText("ROUTE 138")).toBeInTheDocument();
  });

  it("displays an empty message when no routes are found", async () => {
    server.use(
      http.get(apiUrl("/api/v1/routes"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres/:id"), () => HttpResponse.json({ name: "Test Centre" }))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/routes" element={<RoutesPage />} />
      </Routes>,
      { route: "/network/routes", user: buildStaff("Admin") }
    );

    expect(await screen.findByText(/0 routes serving/i)).toBeInTheDocument();
  });

  it("displays an error message when the API returns a 500 error", async () => {
    server.use(
      http.get(apiUrl("/api/v1/routes"), () => new HttpResponse(null, { status: 500 })),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres/:id"), () => HttpResponse.json({ name: "Test Centre" }))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/routes" element={<RoutesPage />} />
      </Routes>,
      { route: "/network/routes", user: buildStaff("Admin") }
    );

    expect(await screen.findByText("Failed to load routes.")).toBeInTheDocument();
  });
});

describe("RouteDetailPage", () => {
  it("displays the route details and status", async () => {
    server.use(
      http.get(apiUrl("/api/v1/routes/route-123"), () => {
        return HttpResponse.json({
          id: "route-123",
          routeNumber: "138",
          name: "Maharagama - Fort",
          isActive: true,
          directions: [],
        });
      }),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres/:id"), () => HttpResponse.json({ name: "Test Centre" }))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/routes/:routeId" element={<RouteDetailPage />} />
      </Routes>,
      { route: "/network/routes/route-123", user: buildStaff("Admin") }
    );

    expect(await screen.findByText("Route 138")).toBeInTheDocument();
    expect(screen.getAllByText("Maharagama - Fort").length).toBeGreaterThan(0);
  });
});

describe("RouteFormPage", () => {
  it("prevents form submission if required fields are missing", async () => {
    const user = userEvent.setup();
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([]))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/routes/new" element={<RouteFormPage />} />
      </Routes>,
      { route: "/network/routes/new", user: buildStaff("Admin") }
    );

    const submitBtn = await screen.findByRole("button", { name: "Create route" });
    await user.click(submitBtn);

    expect(screen.getByLabelText(/Route number/i)).toBeInvalid();
    expect(screen.getByLabelText(/Route name/i)).toBeInvalid();
  });
});
