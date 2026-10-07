import { screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { http, HttpResponse, delay } from "msw";
import { Route, Routes } from "react-router";
import { renderWithProviders } from "@/test/render";
import { server } from "@/test/server";
import { apiUrl } from "@/test/handlers";
import { buildStaff, buildUser } from "@/test/factories";
import { CentresPage } from "@/features/centres/CentresPage";
import { RequireRole } from "@/components/auth/RequireAuth";

describe("CentresPage", () => {
  it("displays the names and districts of centres from the API", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => {
        return HttpResponse.json([
          {
            id: "fake-id-123",
            name: "Colombo Central",
            code: "COL-01",
            city: "Colombo",
            district: "Colombo",
            status: "Active",
          },
        ]);
      }),
      http.get(apiUrl("/api/v1/auth/users"), () => {
        return HttpResponse.json([]);
      })
    );

    renderWithProviders(
      <Routes>
        <Route path="/admin/centres" element={<CentresPage />} />
      </Routes>,
      { route: "/admin/centres", user: buildStaff("CentreManager") }
    );

    expect(await screen.findByText("Colombo Central")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
  });

  it("displays an empty message when no centres are found", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/auth/users"), () => HttpResponse.json([]))
    );

    renderWithProviders(
      <Routes>
        <Route path="/admin/centres" element={<CentresPage />} />
      </Routes>,
      { route: "/admin/centres", user: buildStaff("CentreManager") }
    );

    expect(await screen.findByText("No centres found. Create one to get started.")).toBeInTheDocument();
  });

  it("displays an error message when the API returns a 500 error", async () => {
    server.use(
      http.get(apiUrl("/api/v1/centres"), () => new HttpResponse(null, { status: 500 })),
      http.get(apiUrl("/api/v1/auth/users"), () => HttpResponse.json([]))
    );

    renderWithProviders(
      <Routes>
        <Route path="/admin/centres" element={<CentresPage />} />
      </Routes>,
      { route: "/admin/centres", user: buildStaff("CentreManager") }
    );

    expect(await screen.findByText("Failed to load centres.")).toBeInTheDocument();
  });

  it("blocks access for Commuters and displays an Access Restricted message", async () => {
    renderWithProviders(
      <Routes>
        <Route
          path="/admin/centres"
          element={
            <RequireRole role={["Admin", "CentreManager"]}>
              <CentresPage />
            </RequireRole>
          }
        />
      </Routes>,
      { route: "/admin/centres", user: buildUser({ role: "Commuter" }) }
    );

    expect(await screen.findByText(/Access Restricted/i)).toBeInTheDocument();
  });
});
