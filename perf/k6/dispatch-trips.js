// Component C: Scheduling and Dispatch (owner: chamals3n4).
// The dispatch board under load, dispatchers scheduling trips and moving them through their statuses at the same time,
// and the clash check that stops a bus or driver being booked twice.
//   Targets: board reads p95 < 400 ms, trip writes p95 < 800 ms, clash checks p95 < 800 ms, fewer than 1% failures.
// Run (API on :5250, throwaway database, admin login):
//   K6_EMAIL=admin@upts.lk K6_PASSWORD=... k6 run perf/k6/dispatch-trips.js
// Load: PERF_VUS (default 10) and PERF_DURATION (default 30s) per scenario.
import exec from "k6/execution";
import { check, sleep } from "k6";
import {
  DURATION,
  VUS,
  createNetwork,
  createTrip,
  expectStatus,
  futureTime,
  get,
  login,
  patch,
  pick,
  post,
  serviceDate,
  startAfter,
  summaryTrendStats,
} from "./lib/common.js";

export const options = {
  scenarios: {
    dispatch_board: { executor: "constant-vus", exec: "dispatchBoard", vus: VUS, duration: DURATION },
    trip_lifecycle: { executor: "constant-vus", exec: "tripLifecycle", vus: VUS, duration: DURATION, startTime: startAfter(1) },
    clash_check: { executor: "constant-vus", exec: "clashCheck", vus: VUS, duration: DURATION, startTime: startAfter(2) },
  },
  thresholds: {
    "http_req_duration{scenario:dispatch_board}": ["p(95)<400"],
    "http_req_duration{scenario:trip_lifecycle,kind:write}": ["p(95)<800"],
    "http_req_duration{scenario:clash_check}": ["p(95)<800"],
    "http_req_failed{phase:test}": ["rate<0.01"],
    checks: ["rate>0.99"],
  },
  summaryTrendStats,
};

const BOARD_DAY = 2;
const FIRST_NEW_TRIP_DAY = 5;
// Buses, drivers and bays. Bus i always uses driver i and bay i, so only the timing has to avoid clashes.
const FLEET = 12;

export function setup() {
  const token = login();
  const network = createNetwork(token, "C", { bays: FLEET, vehicles: FLEET, drivers: FLEET });
  // A full day on the board: 36 trips, each bus running every two hours.
  const boardTrips = [];
  for (let k = 0; k < 3 * FLEET; k++) {
    const slot = Math.floor(k / FLEET);
    const i = k % FLEET;
    boardTrips.push({
      id: createTrip(token, network, { vehicle: i, driver: i, bay: i, time: futureTime(BOARD_DAY, 2 + 2 * slot) }),
      vehicle: i,
      slot,
    });
  }
  return { token, network, boardTrips };
}

const read = { phase: "test", kind: "read" };
const write = { phase: "test", kind: "write" };

export function dispatchBoard(data) {
  const { centreId } = data.network;
  expectStatus(get(data.token, `/api/v1/trips?centreId=${centreId}&date=${serviceDate(BOARD_DAY)}`, read), 200, "board for the day");
  expectStatus(get(data.token, `/api/v1/trips?centreId=${centreId}&status=Scheduled`, read), 200, "scheduled trips");
  expectStatus(get(data.token, `/api/v1/trips/${pick(data.boardTrips).id}`, read), 200, "trip detail");
  sleep(0.5);
}

export function tripLifecycle(data) {
  // Every iteration gets its own bus and time slot (90 minutes apart on the same bus), so no two iterations clash.
  const n = exec.scenario.iterationInTest;
  const i = n % FLEET;
  const slot = Math.floor(n / FLEET);
  const minutes = slot * 90;
  const time = futureTime(FIRST_NEW_TRIP_DAY + Math.floor(minutes / 1440), 0, minutes % 1440);
  const { network } = data;

  const created = post(
    data.token,
    "/api/v1/trips",
    {
      centreId: network.centreId,
      routeId: network.routeId,
      vehicleId: network.vehicleIds[i],
      driverId: network.driverIds[i],
      bayId: network.bayIds[i],
      scheduledTime: time,
      notes: "k6 lifecycle",
    },
    write,
  );
  if (!expectStatus(created, 201, "trip scheduled")) return;
  const tripId = created.json("id");
  for (const status of ["Ready", "Boarding", "Dispatched"]) {
    expectStatus(patch(data.token, `/api/v1/trips/${tripId}/status`, { status }, write), 204, `trip moved to ${status}`);
  }
  sleep(0.5);
}

export function clashCheck(data) {
  // Try to put a second trip on a bus that is already running at that time. The API must refuse it every time.
  const existing = pick(data.boardTrips);
  const other = (existing.vehicle + 1) % FLEET;
  const res = post(
    data.token,
    "/api/v1/trips",
    {
      centreId: data.network.centreId,
      routeId: data.network.routeId,
      vehicleId: data.network.vehicleIds[existing.vehicle],
      driverId: data.network.driverIds[other],
      bayId: data.network.bayIds[other],
      scheduledTime: futureTime(BOARD_DAY, 2 + 2 * existing.slot, 15),
      notes: "k6 clash",
    },
    { phase: "test", kind: "clash" },
    400,
  );
  check(res, {
    "clash is refused with 400": (r) => r.status === 400,
    "clash names the vehicle": (r) => r.body.includes("Vehicle is already assigned"),
  });
  sleep(0.5);
}
