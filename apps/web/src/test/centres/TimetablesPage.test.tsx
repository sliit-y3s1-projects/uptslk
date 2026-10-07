import { screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router";
import userEvent from "@testing-library/user-event";
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

  it("shows an error message if the timetable generation API fails", async () => {
    const user = userEvent.setup();
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
  });
});
