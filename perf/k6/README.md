# k6 performance tests

One script per component, plus the two original shared scripts. Each component script builds its own small network
through the API in `setup()` (centre, bays, route, buses, drivers, fare, trips, passengers), so it runs on any empty
database. It writes data, so **only point it at a throwaway database**, never the deployed one.

| Script | Component (owner) | Scenarios | Pass criteria |
|---|---|---|---|
| `centres-network.js` | A – Centres and Network (kishan-ahamed45) | centre list, district filter and search; centre, bay and route details; timetables and directions | p95 < 400 ms |
| `fleet-maintenance.js` | B – Fleet and Maintenance (RashmiK0119) | vehicle list with filters and search; vehicle profile and maintenance history; maintenance records created in parallel | reads p95 < 400 ms, writes p95 < 800 ms |
| `dispatch-trips.js` | C – Scheduling and Dispatch (chamals3n4) | dispatch board for a day; trips scheduled and moved Ready → Boarding → Dispatched in parallel; double-booking a bus is refused | reads p95 < 400 ms, writes and clash checks p95 < 800 ms |
| `bookings-wallet.js` | D – Passengers and Fares (Nadeesha-D-Shalom) | fare quote and seat availability; 200 parallel top-ups to one wallet; book and cancel in parallel | p95 < 1000 ms, final balance exactly 200 × 25, all seats free at the end |
| `read-apis.js` | shared | common list and detail endpoints | lists p95 < 500 ms, details p95 < 400 ms |
| `booking-create.js` | shared | booking creation | p95 < 1000 ms |

Every script also requires fewer than 1% failed requests and more than 99% passing checks. k6 exits with a non-zero
code when a threshold is missed.

## Running

1. Start a throwaway PostgreSQL database and the API on port 5250, for example:

   ```bash
   docker compose up -d
   docker compose exec postgres createdb -U upts_user upts_perf   # a separate, empty database
   cd apps/backend/api
   ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=upts_perf;Username=upts_user;Password=upts_password" \
     dotnet run -c Release --launch-profile http
   ```

   The API migrates the empty database on startup and creates the bootstrap admin.

2. Run a script from the repository root with an admin login:

   ```bash
   K6_EMAIL=admin@upts.lk K6_PASSWORD=<admin password> k6 run perf/k6/dispatch-trips.js
   ```

3. Save the results as evidence:

   ```bash
   k6 run --quiet --no-color --summary-export docs/testing/evidence/k6-dispatch-trips-summary.json \
     perf/k6/dispatch-trips.js > docs/testing/evidence/k6-dispatch-trips.log 2>&1
   ```

| Variable | Default | Meaning |
|---|---|---|
| `K6_BASE_URL` | `http://localhost:5250` | API address |
| `K6_EMAIL`, `K6_PASSWORD` | `admin@upts.lk`, `admin123` | admin login used for setup and the test requests |
| `PERF_VUS` | `10` | virtual users per scenario |
| `PERF_DURATION` | `30s` | length of each scenario, in seconds; scenarios run one after another |
| `PERF_TOPUPS` | `200` | total wallet top-ups in `bookings-wallet.js` |

`K6_VUS` and `K6_DURATION` are reserved by k6 and would override the scenarios, so the scripts use `PERF_*` instead.

## Results on 2026-10-18

Each run used 10 virtual users for 30 seconds per scenario, with a 0.5-second pause between iterations. The API was a
Release build on a local PostgreSQL 16 database in a Linux container. Full output is in `docs/testing/evidence/k6-<script>.log`
and `-summary.json`.

| Script | Requests | Failed | Checks | p95 per scenario |
|---|---|---|---|---|
| `centres-network.js` | 5,291 | 0% | 100% | centre list 6.9 ms, details 8.6 ms, timetables 5.5 ms |
| `fleet-maintenance.js` | 4,152 | 0% | 100% | vehicle list 6.9 ms, profile 6.2 ms, maintenance writes 12.8 ms |
| `dispatch-trips.js` | 4,599 | 0% | 100% | board 15.0 ms, trip lifecycle writes 21.7 ms, clash check 15.7 ms |
| `bookings-wallet.js` | 2,585 | 0% | 100% | fare quote 7.9 ms, top-ups 129.4 ms, book and cancel 20.8 ms; 200 top-ups gave exactly 5,000; all seats free again |

The parallel top-ups are the slowest path because each one locks the wallet row, so they queue. That lock is what keeps
the balance correct.

These numbers show that the code and queries respond quickly under concurrent use on a small dataset. They do not predict
production capacity: the deployed system has a different network, database host and data volume.
