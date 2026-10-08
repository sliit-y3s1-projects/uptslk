import { describe, expect, it } from "vitest";
import { PassengerProfile } from "@/features/riders/components/PassengerProfile";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen } from "@/test/render";
import { server } from "@/test/server";

const passenger = {
  id: "passenger-1",
  fullName: "Nadee Perera",
  phoneNumber: "0771234567",
  email: "nadee@example.com",
  category: "Adult",
  isActive: true,
  wallet: {
    id: "wallet-1",
    balance: 1000,
    transactions: [],
  },
  bookings: [],
};

describe("Wallet payments", () => {
  it("submits a wallet top-up and shows the success state", async () => {
    let requestBody: unknown;
    server.use(
      http.get(apiUrl("/api/v1/passengers/passenger-1"), () =>
        HttpResponse.json(passenger),
      ),
      http.post(apiUrl("/api/v1/passengers/passenger-1/wallet/top-ups"), async ({ request }) => {
        requestBody = await request.json();
        return HttpResponse.json({
          id: "transaction-1",
          balance: 1500,
          type: "Topup",
          amount: 500,
          createdAt: "2026-10-08T10:00:00Z",
        });
      }),
    );

    const { user } = renderWithProviders(<PassengerProfile id="passenger-1" />);

    await screen.findByRole("heading", { name: "Nadee Perera" });
    const amount = screen.getByLabelText("Top-up amount (LKR)");
    await user.type(amount, "500");
    await user.click(screen.getByRole("button", { name: "Top up wallet" }));

    expect(requestBody).toEqual({ amount: 500 });
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Top-up recorded. Balance and transactions refreshed.",
    );
  });

  it("shows the API error when a wallet top-up fails", async () => {
    server.use(
      http.get(apiUrl("/api/v1/passengers/passenger-1"), () =>
        HttpResponse.json(passenger),
      ),
      http.post(apiUrl("/api/v1/passengers/passenger-1/wallet/top-ups"), () =>
        HttpResponse.json({ error: "Wallet limit exceeded" }, { status: 400 }),
      ),
    );

    const { user } = renderWithProviders(<PassengerProfile id="passenger-1" />);

    await screen.findByRole("heading", { name: "Nadee Perera" });
    await user.type(screen.getByLabelText("Top-up amount (LKR)"), "100000");
    await user.click(screen.getByRole("button", { name: "Top up wallet" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Wallet limit exceeded");
  });
});