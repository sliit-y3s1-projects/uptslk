import { describe, expect, it } from "vitest";
import { Route, Routes } from "react-router";
import { RidersPage } from "@/features/riders/RiderPages";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

const passengers = [
  {
    id: "p-1",
    fullName: "Nadeesha Perera",
    phoneNumber: "0771234567",
    email: "nadeesha@example.com",
    category: "Adult",
    balance: 1250,
    bookingCount: 3,
    isActive: true,
  },
  {
    id: "p-2",
    fullName: "Kasun Silva",
    phoneNumber: "0777654321",
    email: "kasun@example.com",
    category: "Student",
    balance: 480,
    bookingCount: 1,
    isActive: true,
  },
];

describe("RidersPage", () => {
  it("shows passengers from the API and narrows the list by search", async () => {
    server.use(
      http.get(apiUrl("/api/v1/passengers"), ({ request }) => {
        const search = new URL(request.url).searchParams.get("search") ?? "";
        const filtered = search
          ? passengers.filter((p) => p.fullName.toLowerCase().includes(search.toLowerCase()))
          : passengers;

        return HttpResponse.json(filtered);
      }),
    );

    const { user } = renderWithProviders(
      <Routes>
        <Route path="/riders/accounts" element={<RidersPage />} />
      </Routes>,
      { route: "/riders/accounts", user: buildStaff("Dispatcher") },
    );

    expect(await screen.findByText("Nadeesha Perera")).toBeInTheDocument();
    expect(screen.getByText("Kasun Silva")).toBeInTheDocument();

    const search = screen.getByLabelText("Search name, phone or email");
    await user.clear(search);
    await user.type(search, "kasun");

    expect(await screen.findByText("Kasun Silva")).toBeInTheDocument();
    expect(screen.queryByText("Nadeesha Perera")).not.toBeInTheDocument();
  });

  it("shows an empty state when no passengers match the current filter", async () => {
    server.use(
      http.get(apiUrl("/api/v1/passengers"), () => HttpResponse.json([])),
    );

    renderWithProviders(
      <Routes>
        <Route path="/riders/accounts" element={<RidersPage />} />
      </Routes>,
      { route: "/riders/accounts", user: buildStaff("Dispatcher") },
    );

    expect(await screen.findByText("No records match this selection.")).toBeInTheDocument();
  });
});
