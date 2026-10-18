// Component D: Passengers and Fares (owner: Nadeesha-D-Shalom).
// Fare quotes and seat checks under load, many top-ups to ONE wallet at the same time, and passengers booking and
// cancelling at once. Teardown checks that no money and no seats were lost on the way.
//   Targets: every request p95 < 1000 ms, fewer than 1% failures, final wallet balance and seat count exactly right.
// Run (API on :5250, throwaway database, admin login):
//   K6_EMAIL=admin@upts.lk K6_PASSWORD=... k6 run perf/k6/bookings-wallet.js
// Load: PERF_VUS (default 10) and PERF_DURATION (default 30s) per scenario; PERF_TOPUPS (default 200) top-ups in total.
import { check, sleep } from "k6";
import {
  DURATION,
  VUS,
  create,
  createNetwork,
  createTrip,
  del,
  expectStatus,
  futureTime,
  get,
  login,
  pick,
  post,
  startAfter,
  summaryTrendStats,
} from "./lib/common.js";

const TOPUPS = Number(__ENV.PERF_TOPUPS || 200);
const TOPUP_AMOUNT = 25;
const FARE = 150;

export const options = {
  scenarios: {
    fare_quote: { executor: "constant-vus", exec: "fareQuote", vus: VUS, duration: DURATION },
    // A fixed number of top-ups shared by all users, so the expected final balance is known.
    wallet_topups: {
      executor: "shared-iterations",
      exec: "walletTopUp",
      vus: VUS,
      iterations: TOPUPS,
      maxDuration: DURATION,
      startTime: startAfter(1),
    },
    book_and_cancel: { executor: "constant-vus", exec: "bookAndCancel", vus: VUS, duration: DURATION, startTime: startAfter(2) },
  },
  thresholds: {
    "http_req_duration{scenario:fare_quote}": ["p(95)<1000"],
    "http_req_duration{scenario:wallet_topups}": ["p(95)<1000"],
    "http_req_duration{scenario:book_and_cancel}": ["p(95)<1000"],
    "http_req_failed{phase:test}": ["rate<0.01"],
    checks: ["rate>0.99"],
  },
  summaryTrendStats,
};

function addPassenger(token, n, balance) {
  const id = create(
    token,
    "/api/v1/passengers",
    {
      fullName: `Perf Passenger ${n}`,
      phoneNumber: `07${String(Date.now() + n).slice(-8)}`,
      category: n % 4 === 0 ? "Student" : "Adult",
    },
    "a passenger",
  );
  if (balance > 0) create(token, `/api/v1/passengers/${id}/wallet/top-ups`, { amount: balance }, "a wallet top-up");
  return id;
}

export function setup() {
  const token = login();
  const network = createNetwork(token, "D");
  create(token, "/api/v1/fare-rules", { routeId: network.routeId, amount: FARE }, "a fare rule");
  const tripId = createTrip(token, network, { time: futureTime(3, 6) });
  const passengerIds = [];
  for (let n = 0; n < 2 * VUS; n++) passengerIds.push(addPassenger(token, n, 100000));
  const walletPassengerId = addPassenger(token, 999, 0);
  return { token, tripId, passengerIds, walletPassengerId };
}

const tags = { phase: "test" };

export function fareQuote(data) {
  const passengerId = pick(data.passengerIds);
  const quote = get(data.token, `/api/v1/fare-rules/quote?tripId=${data.tripId}&passengerId=${passengerId}`, tags);
  expectStatus(quote, 200, "fare quote");
  expectStatus(get(data.token, `/api/v1/bookings/trips/${data.tripId}/seats`, tags), 200, "seat availability");
  sleep(0.5);
}

export function walletTopUp(data) {
  const res = post(data.token, `/api/v1/passengers/${data.walletPassengerId}/wallet/top-ups`, { amount: TOPUP_AMOUNT }, tags);
  expectStatus(res, 201, "wallet top-up");
}

export function bookAndCancel(data) {
  const booked = post(data.token, "/api/v1/bookings", { tripId: data.tripId, passengerId: pick(data.passengerIds), passengerCount: 1 }, tags);
  if (!expectStatus(booked, 201, "booking paid from the wallet")) return;
  const cancelled = del(data.token, `/api/v1/bookings/${booked.json("id")}`, { reason: "k6 performance test" }, tags);
  expectStatus(cancelled, 204, "booking cancelled and refunded");
  sleep(0.5);
}

export function teardown(data) {
  // Every top-up that succeeded must be in the balance: parallel top-ups may not overwrite each other.
  const wallet = get(data.token, `/api/v1/passengers/${data.walletPassengerId}`).json("wallet");
  check(wallet, {
    [`wallet holds all ${TOPUPS} top-ups (${TOPUPS * TOPUP_AMOUNT})`]: (w) => Number(w.balance) === TOPUPS * TOPUP_AMOUNT,
  });
  // Every booking was cancelled again, so the bus must be empty.
  const seats = get(data.token, `/api/v1/bookings/trips/${data.tripId}/seats`).json();
  check(seats, { "all seats are free again after cancelling": (s) => s.occupied === 0 && s.available === s.capacity });
}
