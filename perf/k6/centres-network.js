// Component A: Centres and Network (owner: kishan-ahamed45).
// Read performance of the network pages: the centre list and search, centre and route details, and timetables.
//   Target: every request p95 < 400 ms, fewer than 1% failures.
// Run (API on :5250, throwaway database, admin login):
//   K6_EMAIL=admin@upts.lk K6_PASSWORD=... k6 run perf/k6/centres-network.js
// Load: PERF_VUS (default 10) and PERF_DURATION (default 30s) per scenario.
import { sleep } from "k6";
import { DURATION, VUS, create, startAfter, createNetwork, expectStatus, get, login, pick, summaryTrendStats } from "./lib/common.js";

export const options = {
  scenarios: {
    centre_list: { executor: "constant-vus", exec: "centreList", vus: VUS, duration: DURATION },
    network_detail: { executor: "constant-vus", exec: "networkDetail", vus: VUS, duration: DURATION, startTime: startAfter(1) },
    timetables: {
      executor: "constant-vus",
      exec: "timetables",
      vus: VUS,
      duration: DURATION,
      startTime: startAfter(2),
    },
  },
  thresholds: {
    "http_req_duration{scenario:centre_list}": ["p(95)<400"],
    "http_req_duration{scenario:network_detail}": ["p(95)<400"],
    "http_req_duration{scenario:timetables}": ["p(95)<400"],
    "http_req_failed{phase:test}": ["rate<0.01"],
    checks: ["rate>0.99"],
  },
  summaryTrendStats,
};

const DISTRICTS = ["Colombo", "Gampaha", "Kandy", "Galle", "Kurunegala"];

export function setup() {
  const token = login();
  // Ten more centres across five districts so the list and the filters have something to work on.
  for (let i = 0; i < 10; i++) {
    const district = DISTRICTS[i % DISTRICTS.length];
    create(
      token,
      "/api/v1/centres",
      { code: `A${Date.now().toString(36).slice(-6)}${i}`, name: `${district} Terminal ${i}`, city: district, district, status: "Operating" },
      "a centre",
    );
  }
  const network = createNetwork(token, "A", { bays: 3 });
  const scheduleIds = network.bayIds.map((bayId, i) =>
    create(
      token,
      `/api/v1/routes/${network.routeId}/schedules`,
      { bayId, firstDeparture: `0${5 + i}:00`, lastDeparture: "21:00", headwayMinutes: 30, operatingDays: "Mon,Tue,Wed,Thu,Fri" },
      "a timetable",
    ),
  );
  return { token, network, scheduleIds };
}

const tags = { phase: "test" };

export function centreList(data) {
  expectStatus(get(data.token, "/api/v1/centres", tags), 200, "centre list");
  expectStatus(get(data.token, `/api/v1/centres?district=${pick(DISTRICTS)}`, tags), 200, "centres by district");
  expectStatus(get(data.token, "/api/v1/centres?search=terminal", tags), 200, "centre search");
  sleep(0.5);
}

export function networkDetail(data) {
  const { centreId, routeId } = data.network;
  expectStatus(get(data.token, `/api/v1/centres/${centreId}`, tags), 200, "centre detail");
  expectStatus(get(data.token, `/api/v1/centres/${centreId}/bays`, tags), 200, "bay list");
  expectStatus(get(data.token, `/api/v1/routes/${routeId}`, tags), 200, "route detail with stops");
  expectStatus(get(data.token, "/api/v1/routes", tags), 200, "route list");
  sleep(0.5);
}

export function timetables(data) {
  const { routeId } = data.network;
  expectStatus(get(data.token, `/api/v1/routes/${routeId}/schedules`, tags), 200, "timetable list");
  expectStatus(get(data.token, `/api/v1/routes/${routeId}/directions`, tags), 200, "direction list");
  sleep(0.5);
}
