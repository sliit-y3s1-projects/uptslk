// Shared helpers for the per-component k6 scripts.
// Every script builds its own small network through the API in setup(), so it runs on any empty database.
// Use a throwaway database: the scripts create centres, vehicles, trips, bookings and wallet transactions.
import http from "k6/http";
import { check, fail } from "k6";

export const BASE_URL = (__ENV.K6_BASE_URL || "http://localhost:5250").replace(/\/+$/, "");
export const VUS = Number(__ENV.PERF_VUS || 10);
export const DURATION = __ENV.PERF_DURATION || "30s";
const ADMIN_EMAIL = __ENV.K6_EMAIL || "admin@upts.lk";
const ADMIN_PASSWORD = __ENV.K6_PASSWORD || "admin123";

// Scenarios run one after another so they do not compete. PERF_DURATION is given in seconds, e.g. "30s".
export const startAfter = (scenariosBefore) => `${scenariosBefore * parseInt(DURATION, 10)}s`;

export const summaryTrendStats =["avg", "min", "med", "p(90)", "p(95)", "max"];

// A short suffix that keeps codes, plates and emails unique between runs.
export const runId = () => Date.now().toString(36).toUpperCase().slice(-5);

export function login(email = ADMIN_EMAIL, password = ADMIN_PASSWORD) {
  const res = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({ email, password }), {
    headers: { "Content-Type": "application/json" },
  });
  if (res.status !== 200) fail(`Login as ${email} failed: HTTP ${res.status}`);
  return res.json("token");
}

function params(token, tags, expected) {
  return {
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    tags,
    // A request that is meant to be refused (e.g. a clash) is not counted as a failed request.
    ...(expected ? { responseCallback: http.expectedStatuses(expected) } : {}),
  };
}

export const get = (token, path, tags = {}) => http.get(`${BASE_URL}${path}`, params(token, tags));
export const post = (token, path, body, tags = {}, expected) =>
  http.post(`${BASE_URL}${path}`, JSON.stringify(body ?? {}), params(token, tags, expected));
export const patch = (token, path, body, tags = {}) =>
  http.patch(`${BASE_URL}${path}`, JSON.stringify(body ?? {}), params(token, tags));
export const del = (token, path, body, tags = {}) =>
  http.del(`${BASE_URL}${path}`, body === undefined ? null : JSON.stringify(body), params(token, tags));

// Creates something during setup and returns its id; stops the run if the API refuses.
export function create(token, path, body, what) {
  const res = post(token, path, body, { phase: "setup" });
  if (res.status !== 201 && res.status !== 200) fail(`Setup could not create ${what}: HTTP ${res.status} ${res.body}`);
  return res.json("id");
}

export function expectStatus(res, status, name) {
  return check(res, { [`${name} is ${status}`]: (r) => r.status === status });
}

// Departure times a few days ahead, at whole hours, so they never fall in the past while the test runs.
export function futureTime(daysAhead, hour, minute = 0) {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + daysAhead);
  d.setUTCHours(hour, minute, 0, 0);
  return d.toISOString();
}

export function serviceDate(daysAhead) {
  return futureTime(daysAhead, 6).slice(0, 10);
}

/**
 * One operating centre with bays, a route, buses, drivers and a fare, created through the API as the admin.
 * counts: { bays, vehicles, drivers } (each defaults to 1).
 */
export function createNetwork(token, prefix, counts = {}) {
  const id = runId();
  const code = `${prefix}${id}`.slice(0, 10);
  const centreId = create(
    token,
    "/api/v1/centres",
    { code, name: `${prefix} Perf Centre ${id}`, city: "Colombo", district: "Colombo", status: "Operating" },
    "a centre",
  );
  const bayIds = [];
  for (let i = 0; i < (counts.bays || 1); i++) {
    bayIds.push(create(token, `/api/v1/centres/${centreId}/bays`, { code: `B${i + 1}` }, "a bay"));
  }
  const routeId = create(
    token,
    "/api/v1/routes",
    {
      centreId,
      routeNumber: `${prefix}-${id}`,
      name: "Colombo - Kandy",
      origin: "Colombo",
      destination: "Kandy",
      serviceType: "Normal",
      distanceKm: 115,
      estimatedDurationMin: 60,
      stops: [
        { stopName: "Colombo Fort", latitude: 6.93, longitude: 79.85 },
        { stopName: "Kegalle", latitude: 7.25, longitude: 80.35 },
        { stopName: "Kandy", latitude: 7.29, longitude: 80.63 },
      ],
    },
    "a route",
  );
  const vehicleIds = [];
  for (let i = 0; i < (counts.vehicles || 1); i++) {
    vehicleIds.push(
      create(
        token,
        "/api/v1/vehicles",
        {
          centreId,
          plateNumber: `${code}-${i + 1}`,
          model: i % 2 ? "Lanka Ashok Leyland" : "Isuzu Journey",
          type: "Normal",
          capacity: 52,
          isAccessible: i % 3 === 0,
          status: "Active",
        },
        "a vehicle",
      ),
    );
  }
  const driverIds = [];
  for (let i = 0; i < (counts.drivers || 1); i++) {
    driverIds.push(
      create(
        token,
        "/api/v1/drivers",
        {
          centreId,
          fullName: `Perf Driver ${i + 1}`,
          email: `driver${i + 1}.${code.toLowerCase()}@perf.test`,
          password: "PerfDriver123",
          phoneNumber: `07${String(10000000 + i).slice(-8)}`,
          licenseNumber: `DL-${code}-${i + 1}`,
          status: "Active",
        },
        "a driver",
      ),
    );
  }
  return { centreId, code, bayIds, routeId, vehicleIds, driverIds };
}

export function createTrip(token, network, { vehicle = 0, driver = 0, bay = 0, time }) {
  return create(
    token,
    "/api/v1/trips",
    {
      centreId: network.centreId,
      routeId: network.routeId,
      vehicleId: network.vehicleIds[vehicle],
      driverId: network.driverIds[driver],
      bayId: network.bayIds[bay],
      scheduledTime: time,
      notes: "k6 performance test",
    },
    "a trip",
  );
}

export function pick(list) {
  return list[Math.floor(Math.random() * list.length)];
}
