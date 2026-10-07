// Read-API latency check for the demo dataset.
//   Targets: common list endpoints p95 < 500 ms, detail endpoints p95 < 400 ms.
// Run (API on :5250):
//   k6 run perf/k6/read-apis.js
// Load level: PERF_VUS=100 PERF_DURATION=20s (the names K6_VUS / K6_DURATION are reserved by k6 and override the scenarios).
// Optional authenticated run (sends a Bearer token and also covers /incidents):
//   K6_EMAIL=manager.kadawatha@upts.lk K6_PASSWORD=... k6 run perf/k6/read-apis.js
import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.K6_BASE_URL || "http://localhost:5250";
const VUS = Number(__ENV.PERF_VUS || 10);
const DURATION = __ENV.PERF_DURATION || "30s";

export const options = {
  scenarios: {
    lists: {
      executor: "constant-vus",
      exec: "lists",
      vus: VUS,
      duration: DURATION,
    },
    details: {
      executor: "constant-vus",
      exec: "details",
      vus: VUS,
      duration: DURATION,
      startTime: DURATION, // run after the list scenario so the two do not compete
    },
  },
  thresholds: {
    "http_req_duration{kind:list}": ["p(95)<500"],
    "http_req_duration{kind:detail}": ["p(95)<400"],
    http_req_failed: ["rate<0.01"],
    checks: ["rate>0.99"],
  },
  summaryTrendStats: ["avg", "min", "med", "p(90)", "p(95)", "max"],
};

function headers(token) {
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export function setup() {
  let token = null;
  if (__ENV.K6_EMAIL && __ENV.K6_PASSWORD) {
    const login = http.post(
      `${BASE_URL}/api/v1/auth/login`,
      JSON.stringify({ email: __ENV.K6_EMAIL, password: __ENV.K6_PASSWORD }),
      { headers: { "Content-Type": "application/json" } },
    );
    if (login.status !== 200) throw new Error(`Login failed: HTTP ${login.status}`);
    token = login.json("token");
  }

  const firstId = (path) => {
    const res = http.get(`${BASE_URL}${path}`, { headers: headers(token) });
    const body = res.status === 200 ? res.json() : [];
    return Array.isArray(body) && body.length ? body[0].id : null;
  };
  const ids = {
    trip: firstId("/api/v1/trips"),
    vehicle: firstId("/api/v1/vehicles"),
    route: firstId("/api/v1/routes"),
  };
  if (!ids.trip || !ids.vehicle || !ids.route)
    throw new Error(`Seed data missing, found: ${JSON.stringify(ids)}`);
  return { token, ids };
}

const LISTS = ["/api/v1/trips", "/api/v1/vehicles", "/api/v1/routes", "/api/v1/centres", "/api/v1/drivers"];

export function lists(data) {
  const paths = data.token ? [...LISTS, "/api/v1/incidents"] : LISTS;
  for (const path of paths) {
    const res = http.get(`${BASE_URL}${path}`, {
      headers: headers(data.token),
      tags: { kind: "list", endpoint: path },
    });
    check(res, { "list returned 200": (r) => r.status === 200 });
  }
  sleep(0.2);
}

export function details(data) {
  const paths = [
    `/api/v1/trips/${data.ids.trip}`,
    `/api/v1/vehicles/${data.ids.vehicle}`,
    `/api/v1/routes/${data.ids.route}`,
  ];
  for (const path of paths) {
    const res = http.get(`${BASE_URL}${path}`, {
      headers: headers(data.token),
      tags: { kind: "detail", endpoint: path.replace(/[0-9a-f-]{36}/, ":id") },
    });
    check(res, { "detail returned 200": (r) => r.status === 200 });
  }
  sleep(0.2);
}
