import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { test } from "node:test";
import ts from "typescript";

// Load the real service modules while replacing only the shared HTTP boundary.
const calls = [];
const cache = new Map();
function load(file) {
  file = resolve(file);
  if (cache.has(file)) return cache.get(file);
  const output = ts.transpileModule(readFileSync(file, "utf8"), {
    compilerOptions: {
      module: ts.ModuleKind.CommonJS,
      target: ts.ScriptTarget.ES2022,
    },
  }).outputText;
  const module = { exports: {} };
  new Function("require", "module", "exports", output)(
    (name) => {
      if (name === "@/lib/api/api-client")
        return {
          apiClient: async (path, options = {}) => {
            calls.push({ path, ...options });
          },
        };
      const dependency = name.startsWith("@/")
        ? resolve("src", name.slice(2))
        : resolve(dirname(file), name);
      return load(dependency + ".ts");
    },
    module,
    module.exports,
  );
  cache.set(file, module.exports);
  return module.exports;
}
const { passengersService: passengers } = load(
  "src/features/riders/services/passengers.service.ts",
);
const { fareRulesService: fares } = load(
  "src/features/fares/services/fare-rules.service.ts",
);
const { bookingsService: bookings } = load(
  "src/features/fares/services/bookings.service.ts",
);
const { errorMessage, queryString } = load(
  "src/features/riders/services/request.ts",
);
function check(method, path, body) {
  const actual = calls.pop();
  assert.equal(actual.method ?? "GET", method);
  assert.equal(actual.path, path);
  if (body !== undefined) {
    assert.equal(actual.headers["Content-Type"], "application/json");
    assert.deepEqual(JSON.parse(actual.body), body);
  } else assert.equal(actual.body, undefined);
}
const passengerId = "85761317-4fe9-4299-9377-0f2b08549721";
const tripId = "21de78e8-ce0f-4c69-ae10-1db9676a11f6";

test("passenger mutations preserve exact bodies and keep wallet within passenger", async () => {
  const body = {
    fullName: "Test Passenger",
    phoneNumber: "0771234567",
    email: null,
    category: "Adult",
  };
  await passengers.create(body);
  check("POST", "/api/v1/passengers", body);
  await passengers.update(passengerId, { ...body, isActive: false });
  check("PUT", `/api/v1/passengers/${passengerId}`, {
    ...body,
    isActive: false,
  });
  await passengers.topUp(passengerId, 1000);
  check("POST", `/api/v1/passengers/${passengerId}/wallet/top-ups`, {
    amount: 1000,
  });
  await passengers.deactivate(passengerId);
  check("DELETE", `/api/v1/passengers/${passengerId}`);
});
test("fare update does not send the immutable route relation", async () => {
  const body = { routeId: tripId, passengerCategory: "Student", amount: 180 };
  await fares.create(body);
  check("POST", "/api/v1/fare-rules", body);
  const update = { passengerCategory: "Adult", amount: 200, isActive: true };
  await fares.update(tripId, update);
  check("PUT", `/api/v1/fare-rules/${tripId}`, update);
  await fares.deactivate(tripId);
  check("DELETE", `/api/v1/fare-rules/${tripId}`);
});
test("booking cancellation carries its reason in DELETE, completion uses PATCH", async () => {
  const body = { tripId, passengerId, passengerCount: 2 };
  await bookings.create(body);
  check("POST", "/api/v1/bookings", body);
  await bookings.complete(tripId);
  check("PATCH", `/api/v1/bookings/${tripId}/status`, { status: "Completed" });
  await bookings.cancel(tripId, "Passenger request");
  check("DELETE", `/api/v1/bookings/${tripId}`, {
    reason: "Passenger request",
  });
});
test("filters encode search text and retain false", async () => {
  assert.equal(
    queryString({
      search: "A&B + C",
      active: false,
      category: "",
      ignored: undefined,
    }),
    "?search=A%26B+%2B+C&active=false",
  );
  await fares.quote(tripId, passengerId);
  check(
    "GET",
    `/api/v1/fare-rules/quote?tripId=${tripId}&passengerId=${passengerId}`,
  );
  await bookings.manifest(tripId);
  check("GET", `/api/v1/bookings/trips/${tripId}/manifest`);
});
test("API conflict, validation and network errors are readable", () => {
  assert.equal(
    errorMessage(new Error('{"error":"This departure is full."}')),
    "This departure is full.",
  );
  assert.equal(
    errorMessage(
      new Error(
        '{"errors":{"Amount":["Amount is required."],"Email":["Email is invalid."]}}',
      ),
    ),
    "Amount is required. Email is invalid.",
  );
  assert.equal(
    errorMessage(new Error("Failed to fetch")),
    "Cannot reach the API. Check your connection and try again.",
  );
  assert.equal(
    errorMessage(new Error("Insufficient wallet balance.")),
    "Insufficient wallet balance.",
  );
});
