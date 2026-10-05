// Booking creation latency (the API step before the Stripe redirect).
//   Target: p95 < 1 s.
// WARNING: this writes real bookings and takes seats on the trip. Use a spare
// trip and delete the bookings afterwards.
// Run:
//   K6_EMAIL=<commuter email> K6_PASSWORD=... K6_TRIP_ID=<trip guid> k6 run perf/k6/booking-create.js
import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.K6_BASE_URL || "http://localhost:5250";

export const options = {
  scenarios: {
    book: { executor: "shared-iterations", vus: 1, iterations: Number(__ENV.K6_ITERATIONS || 20), maxDuration: "2m" },
  },
  thresholds: {
    "http_req_duration{kind:booking}": ["p(95)<1000"],
    http_req_failed: ["rate<0.01"],
  },
  summaryTrendStats: ["avg", "min", "med", "p(90)", "p(95)", "max"],
};

export function setup() {
  for (const name of ["K6_EMAIL", "K6_PASSWORD", "K6_TRIP_ID"])
    if (!__ENV[name]) throw new Error(`${name} is required`);
  const login = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: __ENV.K6_EMAIL, password: __ENV.K6_PASSWORD }),
    { headers: { "Content-Type": "application/json" } },
  );
  if (login.status !== 200) throw new Error(`Login failed: HTTP ${login.status}`);
  return { token: login.json("token") };
}

export default function (data) {
  const res = http.post(
    `${BASE_URL}/api/v1/bookings/me`,
    JSON.stringify({ tripId: __ENV.K6_TRIP_ID, passengerCount: 1 }),
    {
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${data.token}` },
      tags: { kind: "booking" },
    },
  );
  check(res, { "booking created (201)": (r) => r.status === 201 });
  sleep(0.5);
}
