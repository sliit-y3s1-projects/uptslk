import { screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router";

import { renderWithProviders } from "@/test/render";
import { server } from "@/test/server";
import { apiUrl } from "@/test/handlers";
import { buildStaff } from "@/test/factories";
import { TimetablesPage } from "@/features/network/TimetablesPage";

describe("TimetablesPage", () => {
  it("displays an empty message when no routes are configured for timetables", async () => {
    server.use(
      http.get(apiUrl("/api/v1/routes"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres/:id/bays"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json([]))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/timetables" element={<TimetablesPage />} />
      </Routes>,
      { route: "/network/timetables", user: buildStaff("Admin") }
    );

    expect(await screen.findByText(/Choose a route to manage its recurring timetables/i)).toBeInTheDocument();
  });

  it("shows the route list failure as the default prompt because the page does not report the error", async () => {
    // Documented behaviour: when GET /api/v1/routes fails the page silently shows the default prompt.
    // See docs/testing/web-centres-defects.md (WEB-A-2).
    server.use(
      http.get(apiUrl("/api/v1/routes"), () => new HttpResponse(null, { status: 500 })),
      http.get(apiUrl("/api/v1/centres"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/centres/:id/bays"), () => HttpResponse.json([])),
      http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json([]))
    );

    renderWithProviders(
      <Routes>
        <Route path="/network/timetables" element={<TimetablesPage />} />
      </Routes>,
      { route: "/network/timetables", user: buildStaff("Admin") }
    );

    expect(await screen.findByText(/Choose a route to manage its recurring timetables/i)).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
