import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { mkdirSync } from "node:fs";
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || "playwright");
const webUrl = process.env.WEB_URL;
if (!webUrl) throw new Error("Set WEB_URL to the running Vite app.");
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
page.setDefaultTimeout(10000);
const pageErrors = [];
page.on("pageerror", (error) => pageErrors.push(error.message));
const guid = (n) => `00000000-0000-4000-8000-${String(n).padStart(12, "0")}`;
const centreId = guid(1),
  routeId = guid(2),
  tripId = guid(3),
  passengerId = guid(4),
  bookingId = guid(5),
  fareId = guid(6);
let passenger = {
  id: passengerId,
  fullName: "Integration Passenger",
  phoneNumber: "0771234567",
  email: null,
  category: "Adult",
  isActive: true,
  balance: 0,
  bookingCount: 0,
};
let transactions = [],
  booking = null,
  fare = null;
let nextBookingError = "",
  listFailure = false,
  networkFailure = false;
let tripStatus = "Scheduled";
let conflictSeat = "";
const trip = {
  id: tripId,
  centreId,
  routeId,
  routeNumber: "138",
  routeName: "Test route",
  vehicle: "TEST-001",
  bay: "B1",
  scheduledTime: "2026-09-16T08:00:00Z",
};
const tripSummary = {
  id: tripId,
  route: trip.routeNumber,
  name: trip.routeName,
  vehicle: trip.vehicle,
  scheduledTime: trip.scheduledTime,
};
const mutationBodies = [];
let createdPassenger = false;
await page.route("**/api/v1/**", async (route) => {
  const request = route.request();
  const url = new URL(request.url());
  const path = url.pathname;
  const method = request.method();
  const body = request.postData() ? request.postDataJSON() : undefined;
  const reply = (data, status = 200) =>
    route.fulfill({
      status,
      contentType: "application/json",
      body: JSON.stringify(data),
    });
  const noContent = () => route.fulfill({ status: 204 });
  if (method !== "GET") mutationBodies.push({ path, method, body });
  if (path === "/api/v1/centres")
    return reply([{ id: centreId, code: "TEST", name: "Integration centre" }]);
  if (path === "/api/v1/routes")
    return reply([
      {
        id: routeId,
        centreId,
        routeNumber: "138",
        name: "Test route",
        isActive: true,
      },
    ]);
  if (path === "/api/v1/trips") return reply([{ ...trip, status: tripStatus }]);
  if (path === `/api/v1/trips/${tripId}`)
    return reply({
      id: tripId,
      status: tripStatus,
      bay: { code: "B1" },
      vehicle: { capacity: 3, isAccessible: true },
    });
  if (path === "/api/v1/passengers") {
    if (method === "POST") {
      passenger = { ...passenger, ...body };
      createdPassenger = true;
      return reply({ id: passengerId }, 201);
    }
    if (networkFailure) return route.abort("failed");
    if (listFailure)
      return reply({ error: "Passenger service unavailable." }, 500);
    const matches =
      (!url.searchParams.get("search") ||
        passenger.fullName
          .toLowerCase()
          .includes(url.searchParams.get("search").toLowerCase())) &&
      (!url.searchParams.get("active") ||
        String(passenger.isActive) === url.searchParams.get("active"));
    return reply(createdPassenger && matches ? [passenger] : []);
  }
  if (path === `/api/v1/passengers/${passengerId}/wallet/top-ups`) {
    passenger.balance += body.amount;
    transactions.unshift({
      id: guid(10 + transactions.length),
      type: "Topup",
      amount: body.amount,
      createdAt: trip.scheduledTime,
    });
    return reply({ balance: passenger.balance }, 201);
  }
  if (path === `/api/v1/passengers/${passengerId}`) {
    if (method === "PUT") {
      passenger = { ...passenger, ...body };
      return noContent();
    }
    if (method === "DELETE") {
      passenger.isActive = false;
      return noContent();
    }
    return reply({
      ...passenger,
      wallet: { id: guid(8), balance: passenger.balance, transactions },
      bookings: booking ? [{ ...booking, tripId, route: "138" }] : [],
    });
  }
  if (path === "/api/v1/fare-rules/quote")
    return fare?.isActive
      ? reply({
          trip: tripSummary,
          passenger: { id: passengerId, category: passenger.category },
          fareRuleId: fareId,
          fare: fare.amount,
        })
      : reply(
          {
            error:
              "No active fare rule exists for this passenger category and route.",
          },
          404,
        );
  if (path === "/api/v1/fare-rules") {
    if (method === "POST") {
      fare = {
        id: fareId,
        ...body,
        route: { routeNumber: "138", name: "Test route", centreId },
        isActive: true,
      };
      return reply({ id: fareId }, 201);
    }
    return reply(fare ? [fare] : []);
  }
  if (path === `/api/v1/fare-rules/${fareId}`) {
    if (method === "PUT") {
      fare = { ...fare, ...body };
      return noContent();
    }
    if (method === "DELETE") {
      fare.isActive = false;
      return noContent();
    }
    return reply(fare);
  }
  if (path === `/api/v1/bookings/trips/${tripId}/seats`)
    return reply(
      [1, 2, 3].map((n) => ({
        seatNumber: String(n),
        isAvailable:
          conflictSeat !== String(n) &&
          !(
            booking &&
            ["Pending", "Confirmed"].includes(booking.status) &&
            booking.seatNumber === String(n)
          ),
      })),
    );
  if (path === `/api/v1/bookings/trips/${tripId}/manifest`)
    return reply({
      trip: tripSummary,
      passengerCount: booking && booking.status !== "Cancelled" ? 1 : 0,
      bookings:
        booking && booking.status !== "Cancelled"
          ? [
              {
                id: booking.id,
                seatNumber: booking.seatNumber,
                passenger: passenger.fullName,
                phoneNumber: passenger.phoneNumber,
                category: passenger.category,
                status: booking.status,
                qrCode: booking.qrCode,
              },
            ]
          : [],
    });
  if (path === "/api/v1/bookings") {
    if (method === "POST") {
      if (nextBookingError) {
        const error = nextBookingError;
        if (error.includes("seat")) conflictSeat = body.seatNumber;
        nextBookingError = "";
        return reply({ error }, error.includes("seat") ? 409 : 400);
      }
      passenger.balance -= fare.amount;
      booking = {
        id: bookingId,
        trip: tripSummary,
        passenger: { ...passenger },
        seatNumber: body.seatNumber,
        fare: fare.amount,
        passengerCategory: passenger.category,
        qrCode: "BKG-0123456789ABCDEF0123456789ABCDEF",
        status: "Confirmed",
        refundAmount: 0,
        transactions: [],
        createdAt: trip.scheduledTime,
      };
      const transaction = {
        id: guid(20),
        bookingId,
        type: "Fare",
        amount: fare.amount,
        createdAt: trip.scheduledTime,
      };
      transactions.unshift(transaction);
      booking.transactions.push(transaction);
      return reply({ id: bookingId }, 201);
    }
    return reply(
      booking
        ? [
            {
              id: bookingId,
              tripId,
              route: "138",
              tripTime: trip.scheduledTime,
              passengerId,
              passenger: passenger.fullName,
              seatNumber: booking.seatNumber,
              fare: booking.fare,
              status: booking.status,
            },
          ]
        : [],
    );
  }
  if (path === `/api/v1/bookings/${bookingId}/seat`) {
    booking.seatNumber = body.seatNumber;
    return noContent();
  }
  if (path === `/api/v1/bookings/${bookingId}/status`) {
    booking.status = body.status;
    return noContent();
  }
  if (path === `/api/v1/bookings/${bookingId}`) {
    if (method === "DELETE") {
      booking.status = "Cancelled";
      booking.cancellationReason = body.reason;
      booking.refundAmount = booking.fare;
      passenger.balance += booking.fare;
      const refund = {
        id: guid(21),
        bookingId,
        type: "Refund",
        amount: booking.fare,
        createdAt: trip.scheduledTime,
      };
      transactions.unshift(refund);
      booking.transactions.push(refund);
      return noContent();
    }
    return reply({ ...booking, passenger: { ...passenger } });
  }
  return reply({ error: `Unexpected test endpoint: ${method} ${path}` }, 404);
});
async function navigate(path) {
  await page.goto(new URL(path, webUrl).href);
  // Existing demo authentication intentionally does not survive a browser reload.
  await page
    .getByRole("button", { name: "Makumbura operations", exact: true })
    .click();
}
const button = (name) => page.getByRole("button", { name, exact: true });
async function visible(text) {
  await page.getByText(text, { exact: true }).first().waitFor();
}
try {
  await navigate("/riders/accounts");
  await visible("No records match this selection.");
  await button("Add passenger").click();
  await page
    .getByLabel("Full name", { exact: true })
    .fill("Integration Passenger");
  await page.getByLabel("Phone number", { exact: true }).fill("0771234567");
  await button("Save passenger").click();
  await visible("Passenger created.");
  await page.getByLabel("Top-up amount (LKR)").fill("1000");
  await button("Top up wallet").click();
  await visible("Top-up recorded. Balance and transactions refreshed.");
  assert.equal(passenger.balance, 1000);
  await button("Edit profile").click();
  await page.getByLabel("Full name", { exact: true }).fill("Updated Passenger");
  await button("Save passenger").click();
  await visible("Passenger updated.");
  await page.getByLabel("Search name, phone or email").fill("missing");
  await visible("No records match this selection.");
  await page.getByLabel("Search name, phone or email").fill("Updated");
  await button("Open profile").waitFor();
  await navigate("/fares/tickets");
  await button("Fare rules").click();
  await page.getByLabel("Centre", { exact: true }).selectOption(centreId);
  await button("Create fare rule").click();
  await page.getByLabel("Route", { exact: true }).selectOption(routeId);
  await page.getByLabel("Fare (LKR)").fill("180");
  await button("Save fare rule").click();
  await visible("Fare rule created.");
  await button("Edit").click();
  await page.getByLabel("Fare (LKR)").fill("200");
  await button("Save fare rule").click();
  await visible("Fare rule updated.");
  await button("Bookings & tickets").click();
  await button("Create booking").click();
  const form = page
    .locator("section")
    .filter({
      has: page.getByRole("heading", { name: "New booking", exact: true }),
    });
  await form.getByLabel("Centre", { exact: true }).selectOption(centreId);
  await page.getByLabel("Scheduled trip", { exact: true }).selectOption(tripId);
  await page.getByLabel("Passenger", { exact: true }).selectOption(passengerId);
  await page.getByRole("button", { name: "Seat 1", exact: true }).click();
  nextBookingError = "Insufficient wallet balance.";
  await button("Confirm seat 1 and pay").click();
  await visible(nextBookingError || "Insufficient wallet balance.");
  nextBookingError =
    "This seat was just booked by another passenger. Select another seat.";
  await button("Confirm seat 1 and pay").click();
  await visible(
    "This seat was just booked by another passenger. Select another seat.",
  );
  assert.equal(
    await page
      .getByRole("button", { name: "Seat 1, unavailable", exact: true })
      .isDisabled(),
    true,
  );
  await page.getByRole("button", { name: "Seat 2", exact: true }).click();
  await button("Confirm seat 2 and pay").click();
  await visible("Booking confirmed. Fare charged to the passenger wallet.");
  assert.equal(passenger.balance, 800);
  await page.getByRole("img", { name: /Ticket QR code BKG-/ }).waitFor();
  if (process.env.SCREENSHOT_DIR) {
    mkdirSync(process.env.SCREENSHOT_DIR, { recursive: true });
    await page.screenshot({
      path: `${process.env.SCREENSHOT_DIR}/booking-ticket.png`,
      fullPage: true,
    });
    await page
      .getByRole("img", { name: /Ticket QR code BKG-/ })
      .screenshot({ path: `${process.env.SCREENSHOT_DIR}/ticket-qr.png` });
  }
  await button("Change seat").click();
  assert.equal(
    await page
      .getByRole("button", { name: "Seat 2, unavailable", exact: true })
      .isDisabled(),
    true,
  );
  await page.getByRole("button", { name: "Seat 3", exact: true }).click();
  await button("Confirm seat change").click();
  await visible("Seat changed. Ticket and manifest refreshed.");
  await page
    .getByRole("link", { name: "Passenger manifest", exact: true })
    .click();
  await visible(booking.qrCode);
  assert.equal(
    await page.getByRole("cell", { name: "3", exact: true }).count(),
    1,
  );
  await page.getByRole("link", { name: "View ticket", exact: true }).click();
  await button("Cancel and refund").click();
  await page.getByLabel("Cancellation reason").fill("Passenger request");
  await button("Confirm cancellation and refund").click();
  await visible("Booking cancelled. Refund and wallet balance refreshed.");
  assert.equal(passenger.balance, 1000);
  await visible("Refund");
  await page.getByRole("link", { name: "Open passenger and wallet" }).click();
  await visible("Refund");
  await button("Deactivate account").click();
  await button("Confirm deactivation").click();
  await visible("Passenger deactivated.");
  assert.equal(passenger.isActive, false);
  await navigate("/fares/tickets");
  await button("Fare rules").click();
  await button("Deactivate").click();
  await button("Confirm deactivation").click();
  await visible("Fare rule deactivated.");
  // Verify completion eligibility independently of cancellation (the API forbids cancelling Completed).
  booking.status = "Confirmed";
  booking.transactions = booking.transactions.filter(
    (t) => t.type !== "Refund",
  );
  booking.refundAmount = 0;
  booking.cancellationReason = null;
  transactions = transactions.filter((t) => t.type !== "Refund");
  passenger.balance = 800;
  tripStatus = "Dispatched";
  await navigate(`/fares/tickets?bookingId=${bookingId}`);
  await button("Complete booking").click();
  await button("Confirm completion").click();
  await visible("Booking completed.");
  assert.equal(booking.status, "Completed");
  assert.equal(await button("Cancel and refund").count(), 0);
  booking.status = "Cancelled";
  booking.refundAmount = 200;
  passenger.balance = 1000;
  transactions.unshift({
    id: guid(21),
    bookingId,
    type: "Refund",
    amount: 200,
    createdAt: trip.scheduledTime,
  });
  for (const path of [
    "/riders/support",
    "/passengers/assistance",
    "/fares/payments",
    "/fares/reconciliation",
  ]) {
    await navigate(path);
    await button("Open profile").waitFor();
  }
  listFailure = true;
  await navigate("/riders/accounts");
  await visible("Passenger service unavailable.");
  listFailure = false;
  await button("Try again").click();
  await button("Open profile").waitFor();
  networkFailure = true;
  await navigate("/riders/accounts");
  await visible("Cannot reach the API. Check your connection and try again.");
  networkFailure = false;
  await button("Try again").click();
  await button("Open profile").waitFor();
  await button("Open profile").click();
  await visible("Refund");
  if (process.env.SCREENSHOT_DIR) {
    mkdirSync(process.env.SCREENSHOT_DIR, { recursive: true });
    await page.screenshot({
      path: `${process.env.SCREENSHOT_DIR}/passenger-wallet.png`,
      fullPage: true,
    });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({
      path: `${process.env.SCREENSHOT_DIR}/passenger-mobile.png`,
      fullPage: true,
    });
    assert.equal(
      await page.evaluate(
        () => document.documentElement.scrollWidth > innerWidth,
      ),
      false,
      "Mobile page should not overflow horizontally",
    );
  }
  assert.deepEqual(
    mutationBodies.find(
      (r) => r.path === "/api/v1/bookings" && r.method === "POST",
    ).body,
    { tripId, passengerId, seatNumber: "1" },
  );
  assert.deepEqual(
    mutationBodies.find((r) => r.path.endsWith("/status")).body,
    { status: "Completed" },
  );
  assert.deepEqual(
    mutationBodies.find(
      (r) =>
        r.path === `/api/v1/bookings/${bookingId}` && r.method === "DELETE",
    ).body,
    { reason: "Passenger request" },
  );
  assert.deepEqual(pageErrors, []);
  console.log(
    "PASS: Passenger/fare CRUD, wallet refresh, bookings, 400/409 errors, real-capacity seat UI, QR, manifest, cancellation/refund, completion, support views, empty and network/error states.",
  );
} catch (error) {
  console.error("Browser errors:", pageErrors);
  console.error((await page.locator("body").innerText()).slice(-4000));
  throw error;
} finally {
  await browser.close();
}
