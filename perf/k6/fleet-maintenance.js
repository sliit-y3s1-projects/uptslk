// Component B: Fleet and Maintenance (owner: RashmiK0119).
// The fleet pages under load: the vehicle list with filters, vehicle profiles with their maintenance history,
// and fleet officers booking maintenance at the same time.
//   Targets: reads p95 < 400 ms, maintenance writes p95 < 800 ms, fewer than 1% failures.
// Run (API on :5250, throwaway database, admin login):
//   K6_EMAIL=admin@upts.lk K6_PASSWORD=... k6 run perf/k6/fleet-maintenance.js
// Load: PERF_VUS (default 10) and PERF_DURATION (default 30s) per scenario.
import { sleep } from "k6";
import {
  DURATION,
  VUS,
  createNetwork,
  expectStatus,
  futureTime,
  get,
  login,
  pick,
  post,
  startAfter,
  summaryTrendStats,
} from "./lib/common.js";

export const options = {
  scenarios: {
    vehicle_list: { executor: "constant-vus", exec: "vehicleList", vus: VUS, duration: DURATION },
    vehicle_profile: { executor: "constant-vus", exec: "vehicleProfile", vus: VUS, duration: DURATION, startTime: startAfter(1) },
    maintenance_writes: { executor: "constant-vus", exec: "maintenanceWrites", vus: VUS, duration: DURATION, startTime: startAfter(2) },
  },
  thresholds: {
    "http_req_duration{scenario:vehicle_list}": ["p(95)<400"],
    "http_req_duration{scenario:vehicle_profile}": ["p(95)<400"],
    "http_req_duration{scenario:maintenance_writes,kind:write}": ["p(95)<800"],
    "http_req_failed{phase:test}": ["rate<0.01"],
    checks: ["rate>0.99"],
  },
  summaryTrendStats,
};

const TYPES = ["Oil change", "Brake inspection", "Tyre rotation", "Engine service"];

export function setup() {
  const token = login();
  // A fleet of 25 buses at one centre. No trips, so maintenance never clashes with a duty.
  const network = createNetwork(token, "B", { vehicles: 25 });
  return { token, network };
}

const read = { phase: "test", kind: "read" };
const write = { phase: "test", kind: "write" };

export function vehicleList(data) {
  const { centreId } = data.network;
  expectStatus(get(data.token, `/api/v1/vehicles?centreId=${centreId}`, read), 200, "vehicle list for a centre");
  expectStatus(get(data.token, `/api/v1/vehicles?centreId=${centreId}&status=Active`, read), 200, "active vehicles");
  expectStatus(get(data.token, "/api/v1/vehicles?search=leyland", read), 200, "vehicle search");
  sleep(0.5);
}

export function vehicleProfile(data) {
  const vehicleId = pick(data.network.vehicleIds);
  expectStatus(get(data.token, `/api/v1/vehicles/${vehicleId}`, read), 200, "vehicle profile");
  expectStatus(get(data.token, `/api/v1/maintenance-records?vehicleId=${vehicleId}`, read), 200, "maintenance history");
  sleep(0.5);
}

export function maintenanceWrites(data) {
  const vehicleId = pick(data.network.vehicleIds);
  // Spread bookings over the next 60 days so the history grows realistically.
  const when = futureTime(1 + Math.floor(Math.random() * 60), 1 + Math.floor(Math.random() * 20));
  const res = post(
    data.token,
    "/api/v1/maintenance-records",
    { vehicleId, type: pick(TYPES), description: "Scheduled by the k6 performance test.", scheduledFor: when },
    write,
  );
  expectStatus(res, 201, "maintenance record created");
  expectStatus(get(data.token, `/api/v1/maintenance-records?centreId=${data.network.centreId}&status=Scheduled`, read), 200, "scheduled maintenance list");
  sleep(0.5);
}
