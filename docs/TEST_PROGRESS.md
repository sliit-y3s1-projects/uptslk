# UPTS Test Progress

Last updated: 2026-10-07, after the coverage round (branch `backend-tests`, nothing committed yet)

Legend: ✅ Done and verified · 🟡 In progress / exists but not yet verified by us · ⬜ Not started

## 1. Progress by testing area (from the assignment brief)

| # | Testing area | Status | What exists now | What is still missing |
|---|---|---|---|---|
| 1 | **Backend / API testing** (unit, service logic, validation, controller, auth, API integration) | ✅ Done | **501 xUnit tests, all passing** (`apps/api.Tests`). **Code coverage of the API: 74% of lines, 62.9% of branches** (was 53% / 38.5% before this round); HTML report in `docs/testing/coverage-report/`. Every controller except three is above 84%: Fare rules, Incidents, Notifications and Payments 100%; Routes, Drivers, Bookings, Support and Trips 98%+; Auth 91%; Maintenance 86%; Centres 85%. The three below are AgentRecovery (16.5%), Passengers (63%) and Vehicles (69%). Covers CRUD, validation, business rules, role-based and centre-based access, real JWT login/refresh, Stripe checkout/webhooks/refunds with a fake gateway, and HTTP integration through `WebApplicationFactory`. **13 defects found and fixed** (DEF-1 to DEF-13). | Not covered: the agent-recovery workflow classes (`RecoveryWorkflowService`, `GeminiRecoveryPlanner`, `AgentRecoveryController`: 724 uncovered lines, the Agentic AI step), the real Stripe and Supabase network clients (need the live services), `PassengersController` 63% and `VehiclesController` 69% (profile-photo/vehicle-image upload and password-reset branches). Moq is not used (fakes are written by hand). |
| 2 | **Database testing** (integration, constraints, relationships, migrations, transactions) | ✅ Done | 64 tests in `apps/api.Tests/Database` on **real PostgreSQL 17 via Testcontainers**, all passing: 16 migration (apply to empty DB, idempotent, rollback to zero and forward, no model drift, column types, money precision, partial index), 21 constraint (unique, foreign key, not-null, length, numeric range, restrict deletes), 11 relationship (graph loading, cascade / set-null rules, 1:1 wallet, UTC round trip), 6 transaction (commit, rollback, atomic SaveChanges, isolation), and 10 parallel-request reliability tests through the real API (bookings, wallets, card checkout, refunds, top-ups). Defects DEF-3 to DEF-6 and DEF-9, DEF-10, DEF-13 were found here. | Needs Docker to run. The Stripe webhook handlers and recovery approvals were not tested under parallel load. CI does not run the tests yet. |
| 3 | **React web testing** (components, forms, protected routes, API integration, UI states) | ✅ Done | **206 Vitest tests in 29 files, all passing** (`apps/web`, React Testing Library + MSW): 10 foundation self-checks plus one folder per member, all five categories covered in each: A Centres 36, B Fleet 29, C Dispatch 92, D Bookings 39. Coverage of each member's own features: Centres/Network 45% of statements, Fleet 63%, Bookings/Fares/Riders 46%, Dispatch/Drivers 85%. **11 web defects found**: 9 fixed (WEB-B-1, WEB-C-1 to C-7, WEB-D-1) and 2 recorded as open (WEB-A-1, WEB-A-2). Per member: `docs/testing/web-<folder>-test-cases.md` and `-defects.md`, evidence in `docs/testing/evidence/web-*`. | No Playwright browser tests. Whole-web-app coverage is 47.6% of statements (48.9% of lines, 44.6% of branches, measured 2026-10-08); areas outside the four members' features (landing, super-admin, agent recovery, reports, settings) have no tests. `src/features/fares/tests/contracts.test.mjs` still uses Node's own runner and is not part of Vitest. |
| 4 | **Flutter mobile testing** (unit, widget, forms, navigation, API integration) | 🟡 Run, passing | 7 test files in `apps/mobile/test` (auth/booking/driver API services, booking eligibility, journey search widget, profile edit, widget_test). **`flutter test`: 23 passed, 0 failed** (`docs/testing/evidence/flutter-test.log`). | No `integration_test/` folder. No coverage report. Tests are not split per component. |
| 5 | **Integration / End-to-End testing** (cross-component, full business workflow, cross-platform) | ⬜ Not started | Backend HTTP integration tests partly cover cross-component flows (trip → booking → wallet). | One full workflow test is needed (e.g. manager creates trip → commuter books → breakdown → recovery). Tool still to be chosen (Playwright / Postman-Newman / Flutter integration_test). |
| 6 | **Non-functional testing** (performance, load, stress, security, usability, accessibility, compatibility, reliability, recovery) | 🟡 Partial | **Performance / load (k6, done):** read APIs p95 71 ms (list) and 80 ms (detail) at 10 users, targets 500 / 400 ms; booking create p95 58 ms, target 1000 ms; 0 failed requests. **Stress (k6, done):** 100 users, 33,157 requests at about 816 req/s, 0 failures, list p95 206 ms, detail p95 339 ms (limit 400 ms, close). **Security (done):** 23 authorization tests behind DEF-1, `dotnet list package --vulnerable` (API clean), `pnpm audit` (37 findings, see bug_found.txt T-2). **Reliability (done):** parallel booking / cancel tests on PostgreSQL found and fixed 3 high defects. | No OWASP ZAP, Lighthouse or axe runs. Usability, accessibility, compatibility and recovery testing not started. k6 ran against a throwaway local database, not the deployed system. The read script covers 5 list and 3 detail endpoints only. |
| 7 | **Agentic AI testing and evaluation** (task completion, tool selection, structured output, rule compliance, prompt injection, approval, failure recovery, safe failure) | 🟡 Exists, needs gap review | 45 tests in `apps/api.Tests/AgentRecovery` (validator, composer, planning, tools, security, HTTP) plus the evaluation runner `apps/api.Evaluation` and baseline results in `docs/evaluation/results`. | We have not mapped them to each listed category (e.g. prompt injection, safe failure), so gaps are unknown. Live-model evaluation run is not done. |
| 8 | **Submission documents** (report PDF, test cases, defect report, execution summary, AI usage declaration, tool evidence) | 🟡 Started | `docs/bug_found.txt` (13 defects + 5 tooling findings, each with steps, fix and retest evidence), before/after logs and TRX files and the coverage report in `docs/testing/`, `apps/api.Tests/README.md`. | Test plan, test case document (expected/actual/pass-fail), execution summary, PDF report, CLEAR AI declaration, per-member contribution evidence. |

## 2. Backend progress by team component

| Component | Owner | Test folder | Tests | Status | Notes |
|---|---|---|---|---|---|
| A: Centres and Network | kishan-ahamed45 | `api.Tests/Centres/` | 67 | ✅ Backend done | Centre, bay, route, direction, timetable CRUD, trip generator, departure-time rules, access control. DEF-2 and DEF-11 found here. |
| B: Fleet and Maintenance | RashmiK0119 | `api.Tests/Fleet/` | 34 | ✅ Backend done | Vehicle CRUD, maintenance records, access control. Includes the 3 existing maintenance-rule tests. |
| C: Scheduling and Dispatch | chamals3n4 | `api.Tests/Dispatch/` | 105 | ✅ Backend done | Trip CRUD, conflicts, status state machine, drivers (management and self-service), incidents with centre isolation, access control. |
| D: Passengers and Fares | Nadeesha-D-Shalom | `api.Tests/Bookings/` | 125 | ✅ Backend done | Booking, wallet, fares, passengers, commuter `/me` endpoints, card checkout, Stripe webhooks, refunds, support requests, notifications, access control. Includes 6 existing eligibility tests. |
| Shared agent workflow | whole team | `api.Tests/AgentRecovery/` | 45 | 🟡 Exists | Not reviewed against the agentic-testing categories yet. |
| Shared harness | whole team | `api.Tests/Shared/` | n/a | ✅ Done | `TestApiFactory`, `RealAuthApiFactory` (real JWT), `FakePaymentGateway`, `StripeWebhook` signer, `TestApiExtensions`, `TestWorld`. |
| Authentication and isolation (all components) | whole team | `api.Tests/Auth/` | 61 | ✅ Done | Register, login, tokens, refresh, logout, account management, admin user management, centre-to-centre isolation. DEF-7 and DEF-12 found here. |
| Database (all components) | whole team | `api.Tests/Database/` | 64 | ✅ Done | Real PostgreSQL via Testcontainers. Concurrency tests belong to component D; DEF-6 touches component C. |

Web tests per component: ✅ all four members have their own folder and documents (see row 3). Mobile tests are general and not split by component.

## 3. Defects

| ID | Title | Severity | Status | Retest |
|---|---|---|---|---|
| DEF-1 | Write endpoints (centres, routes, vehicles, trips, passengers, bookings, fares, refunds, support requests, drivers) had no authentication/authorization | High | Fixed | Pass (23 tests) |
| DEF-2 | Route / direction update crashed with HTTP 500 | Medium | Fixed | Pass (2 tests) |
| DEF-3 | Parallel bookings oversold a trip (5 seats sold, capacity 2) | High | Fixed | Pass |
| DEF-4 | Parallel bookings spent the same wallet balance twice (8 tickets for 100) | High | Fixed | Pass |
| DEF-5 | Parallel cancellations created 7 refunds for one booking | High | Fixed | Pass |
| DEF-6 | Trip time without a timezone returned HTTP 500 on PostgreSQL | Medium | Fixed | Pass |
| DEF-7 | Malformed refresh cookie returned HTTP 500 | Low | Fixed | Pass |
| DEF-8 | Stripe webhook without a signature header returned HTTP 500 | Low | Fixed | Pass |
| DEF-9 | Card checkout held 10 seats on a 2-seat vehicle | High | Fixed | Pass |
| DEF-10 | Parallel refund requests asked Stripe 6 times for one booking | High | Fixed | Pass |
| DEF-11 | Timetable ending near midnight made trip generation loop forever | High | Fixed | Pass |
| DEF-12 | A manager could change another centre's centres, routes, vehicles, trips and fares | High | Fixed | Pass (7 tests failed before) |
| DEF-13 | Ten parallel wallet top-ups of 100 gave a balance of 300 | High | Fixed | Pass |

Tooling and configuration findings (details in `docs/bug_found.txt`):

| ID | Finding | Severity | Status |
|---|---|---|---|
| T-1 | k6 script used reserved variable names and ignored load settings | Low | Fixed |
| T-2 | Web dependencies: 37 audit findings (1 critical, 13 high); 34 come only through `shadcn`, which is in `dependencies` | Medium | Open |
| T-3 | Test project pulls a vulnerable SQLite native package (not deployed) | Low | Open |
| T-4 | Development secrets file is ignored now but may exist in git history | Low | Open |
| T-5 | Bootstrap admin falls back to a well-known default password | Medium | Open |

Details and reproduction steps: `docs/bug_found.txt`. Evidence: `docs/testing/evidence/`.

## 4. Open items and risks

- **Nothing is committed.** Work is on branch `backend-tests`. Each member should commit their own folder under their own account so contribution evidence is correct.
- **DEF-1 fix changes production authorization.** Role lists need team confirmation, and the web app has not been run against them.
- **DEF-3 to DEF-13 fixes change production code** (row locks in `BookingsController`, `BookingPaymentService` and `PassengersController`; a UTC date converter in `AppDbContext`; `ScheduleTimes`; `CentreAccess`). No schema change or migration was needed. DEF-6 assumes a time sent without an offset means UTC. **DEF-12 means staff with no centre assigned can no longer change centre data**; the team should confirm that is intended.
- **Test counts** come from `docs/testing/evidence/full-suite-final.trx` (theory cases counted individually): Centres 67 + Fleet 34 + Dispatch 105 + Bookings 125 + Auth 61 + AgentRecovery 45 + Database 64 = 501, all passing.
- **Docker is required** for the 64 database tests. Without Docker use `--filter "Category!=Database"` (437 tests).
- **Web tests were written partly by AI on Chamal's instruction.** The test-case documents say so (CLEAR declarations). Kishan, Rashmi and Nadeesha must read the parts added for them and add or confirm their own account before submission. Chamal must do the same for Component C.
- **T-2 to T-5 are open** (dependency versions, secrets and the bootstrap password). They need a team decision and are not fixed in this branch.
- **k6 numbers** come from a laptop with a local database seeded with 24 trips, 8 vehicles and 8 drivers; they show the code is fast, not that production will be.

## 5. Suggested order for the remaining work

1. ~~Run k6, `dotnet list package --vulnerable`, `pnpm audit`~~ ✅ done, output saved.
2. ~~Run the existing Flutter tests~~ ✅ done (23 passed).
3. ~~Set up Vitest + React Testing Library and write the web tests~~ ✅ done (206 tests).
4. ~~Add a PostgreSQL (Testcontainers) suite~~ ✅ done (64 tests).
5. Map the agent tests to the brief's categories and fill any gaps. 🟡
6. One end-to-end workflow. ⬜
7. Generate the report documents (test plan, test cases, execution summary, CLEAR declaration, PDF). ⬜

## How to rerun

```bash
# code coverage (open docs/testing/coverage-report/index.html afterwards)
cd apps && dotnet test api.Tests/api.Tests.csproj --settings api.Tests/coverage.runsettings --results-directory api.Tests/TestResults/cov
cd apps && dotnet test api.Tests/api.Tests.csproj                       # everything, 501 tests (needs Docker)
cd apps && dotnet test api.Tests/api.Tests.csproj --filter "Category=Database"    # PostgreSQL / Testcontainers only, 64 tests
cd apps && dotnet test api.Tests/api.Tests.csproj --filter "Category!=Database"   # no Docker needed, 437 tests
cd apps && dotnet test api.Tests/api.Tests.csproj --filter "FullyQualifiedName~api.Tests.Fleet"   # one component
cd apps/web && pnpm test                                                  # web, 206 tests
cd apps/web && pnpm test:coverage                                         # web coverage (apps/web/coverage)
cd apps/mobile && flutter test                                           # mobile, 23 tests
k6 run perf/k6/read-apis.js                                              # performance (API on :5250; set K6_EMAIL / K6_PASSWORD for an admin login)
PERF_VUS=100 PERF_DURATION=20s k6 run perf/k6/read-apis.js               # stress run
K6_EMAIL=... K6_PASSWORD=... K6_TRIP_ID=... k6 run perf/k6/booking-create.js   # writes real bookings, use a throwaway database
```
