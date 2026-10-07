import { screen, waitFor } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { http, HttpResponse } from "msw";
import { Route, Routes } from "react-router";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test/render";
import { server } from "@/test/server";
import { apiUrl } from "@/test/handlers";
import { buildStaff } from "@/test/factories";
import { CentreFormPage } from "@/features/centres/CentresPage";

describe("CentreFormPage", () => {
  it("shows validation errors when required fields are empty", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <Routes>
        <Route path="/admin/centres/new" element={<CentreFormPage />} />
      </Routes>,
      { route: "/admin/centres/new", user: buildStaff("Admin") }
    );

    // Try to submit the empty form
    await user.click(screen.getByRole("button", { name: "Create centre" }));

    // Verify HTML5 validation marks the required fields as invalid
    expect(screen.getByLabelText(/Centre code/i)).toBeInvalid();
    expect(screen.getByLabelText(/Centre name/i)).toBeInvalid();
    expect(screen.getByLabelText(/City/i)).toBeInvalid();
  });

  it("sends a POST request and navigates on successful creation", async () => {
    const user = userEvent.setup();
    let requestBody: unknown;
    
    server.use(
      http.post(apiUrl("/api/v1/centres"), async ({ request }) => {
        requestBody = await request.json();
        return HttpResponse.json({ id: "new-centre-123" });
      })
    );

    renderWithProviders(
      <Routes>
        <Route path="/admin/centres/new" element={<CentreFormPage />} />
        <Route path="/admin/centres/new-centre-123" element={<div>Success Navigation</div>} />
      </Routes>,
      { route: "/admin/centres/new", user: buildStaff("Admin") }
    );

    // Fill out the required fields
    await user.type(screen.getByLabelText(/Centre code/i), "KAN-01");
    await user.type(screen.getByLabelText(/Centre name/i), "Kandy Central");
    await user.type(screen.getByLabelText(/City/i), "Kandy");
    await user.type(screen.getByLabelText(/District/i), "Kandy");

    // Submit the form
    await user.click(screen.getByRole("button", { name: "Create centre" }));

    // Verify the POST request had the right data
    await waitFor(() => {
      expect(requestBody).toMatchObject({
        code: "KAN-01",
        name: "Kandy Central",
        city: "Kandy",
        district: "Kandy",
        status: "Planned", // Default value from the Select component
      });
    });

    // Verify it navigated to the new centre's profile
    expect(await screen.findByText("Success Navigation")).toBeInTheDocument();
  });

  it("displays an error message when the API returns a 409 Conflict (duplicate code)", async () => {
    const user = userEvent.setup();
    
    server.use(
      http.post(apiUrl("/api/v1/centres"), () => {
        return new HttpResponse(null, { status: 409 });
      })
    );

    renderWithProviders(
      <Routes>
        <Route path="/admin/centres/new" element={<CentreFormPage />} />
      </Routes>,
      { route: "/admin/centres/new", user: buildStaff("Admin") }
    );

    await user.type(screen.getByLabelText(/Centre code/i), "DUPE");
    await user.type(screen.getByLabelText(/Centre name/i), "Dupe Centre");
    await user.type(screen.getByLabelText(/City/i), "City");
    await user.type(screen.getByLabelText(/District/i), "District");

    await user.click(screen.getByRole("button", { name: "Create centre" }));

    // Check for the error message shown in the UI
    expect(await screen.findByText("Failed to save centre. Please try again.")).toBeInTheDocument();
  });
});
