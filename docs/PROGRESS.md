# UPTSLK Project Progress

Last reviewed: 23 September 2026

`✅ IMPLEMENTED` means the API, database model, and/or React interface exists in the repository. It does **not** mean every workflow has been demonstrated with a populated local database. The operational demo data was deliberately cleared during the route-direction refactor, so a fresh realistic setup is required for final end-to-end verification.

## Foundation

- ✅ IMPLEMENTED — ASP.NET Core 8 API, PostgreSQL/EF Core migrations, React/Vite administrator application, shared API client, authentication, and reusable UI components.
- ✅ IMPLEMENTED — Role-aware commuter, centre-operations, fleet, network, passenger, and fare routes.
- ✅ IMPLEMENTED — Stripe Checkout integration, signed webhook handling, payment-status polling, payment reconciliation, and refund workflow.
- ✅ IMPLEMENTED — Capacity-based urban booking model. A booking holds a passenger count and boarding pass; it does not reserve a numbered seat.
- ⏳ REMAINING — Final review of role/centre data isolation and production-safe configuration. Development Stripe credentials must not be committed for a real deployment.

## Component A — Centres and Network

Owner: Kishan Ahamed

- ✅ IMPLEMENTED — Centre CRUD, status/district filtering, centre profiles, bay CRUD, and bay availability management.
- ✅ IMPLEMENTED — Route CRUD, ordered stops, route directions, terminal-centre relationships, and active/inactive direction controls.
- ✅ IMPLEMENTED — Timetable CRUD with recurring service patterns and daily trip generation.
- ✅ IMPLEMENTED — API-connected React pages for centres, routes, bays, stops, directions, and timetables.
- ⏳ REMAINING — Create final real centres, stops, directions, bays, and timetables for the demonstration database; then perform a browser CRUD walkthrough including validation/error states.

## Component B — Fleet and Maintenance

Owner: Rashmi

- ✅ IMPLEMENTED — Vehicle CRUD, centre assignment, vehicle type, passenger capacity, accessibility, status, and detail screens.
- ✅ IMPLEMENTED — Driver CRUD, driver-to-centre assignment, licence/status management, and trip-selection data.
- ✅ IMPLEMENTED — Maintenance records, readiness states, filtering, and API-connected fleet screens.
- ⏳ REMAINING — Populate demonstration vehicles/drivers, complete a maintenance/readiness walkthrough, and verify reassignment behaviour against live scheduled trips.

## Component C — Scheduling and Dispatch

Owner: Chamal

- ✅ IMPLEMENTED — Trip scheduling, schedule editing, conflict validation, route-direction-aware dispatch, trip history, lifecycle actions, cancellation, reassignment, and delay handling.
- ✅ IMPLEMENTED — Dispatch board with Attention, Boarding, Ready & scheduled, and Dispatched views; trip detail and incident-report flows use persisted API data.
- ✅ IMPLEMENTED — Recurring timetable generation, daily departure generation, operational bay board, capacity overview, and a daily duty-roster view derived from assigned trips.
- ✅ IMPLEMENTED — Incident list/detail/reporting workflow and centre operations UX improvements.
- ⏳ REMAINING — Demonstrate a realistic disruption: report incident, reassign a valid bus/driver/bay, progress lifecycle, and verify history. Driver Flutter integration is also not yet implemented.

## Component D — Passengers and Fares

Owner: Nadeesha D. Shalom

- ✅ IMPLEMENTED — Passenger CRUD, profile management, wallet top-ups, transaction history, fare-rule CRUD, booking list/filtering, digital QR tickets, manifests, cancellation, and refunds.
- ✅ IMPLEMENTED — Commuter journey search, one-way checkout flow, My Tickets page, Stripe Checkout redirect, webhook confirmation, and payment-status page.
- ✅ IMPLEMENTED — Staff and commuter booking screens now use passenger counts and available vehicle capacity; legacy seat selection/change flows are removed from active UI and API responses.
- ✅ IMPLEMENTED — Fare and capacity checks multiply the fare by passenger count and block a booking when the group exceeds remaining capacity.
- ⏳ REMAINING — Run the live booking → Stripe test payment → webhook → confirmed ticket → manifest → refund scenario after rebuilding routes, trips, fare rules, and commuter accounts.

## Shared Future Work

- ⏳ NOT STARTED — Flutter commuter and driver applications, including driver assignment, lifecycle update, and incident reporting.
- ⏳ NOT STARTED — Persisted Agentic AI recovery workflow: workflow tables, allow-listed tools, deterministic validation, manager approval, audit trail, and execution states.
- ⏳ REMAINING — Final reports/dashboard metrics, pagination where operational lists grow large, security review, screenshots, ADRs, and member demonstration evidence.

## Current Verification

- ✅ `dotnet build` succeeds for `apps/api` (with existing package-version resolution warnings for EF Core/Npgsql 8.0.130).
- ✅ `pnpm build` succeeds for `apps/web` (with existing Vite configuration/chunk-size warnings).
- ✅ Fare/passenger contract tests pass: `node src/features/fares/tests/contracts.test.mjs`.
- ✅ Database migration added for booking passenger counts: `20260923065508_AddBookingPassengerCount`.
- ⏳ REQUIRED — Full API/browser end-to-end verification cannot be claimed until a coherent operational dataset is recreated. The database currently has no centres, routes, bays, vehicles, drivers, fares, or trips by design.
- ⚠️ `pnpm lint` currently has unrelated existing errors in `ProfilePage.tsx` and `lib/api/api-client.ts`; the custom date/time picker lint errors were resolved.

## End-to-End Demonstration Sequence

1. Create two terminal centres, bays, a route, ordered stops, and both route directions.
2. Register buses with passenger capacity, create drivers, and make them active/ready.
3. Create a recurring timetable for each direction, then generate a service day's trips.
4. Assign/reassign a bay, bus, and driver; confirm the trips appear in the daily duty roster and dispatch board.
5. Create an active fare rule and a commuter account.
6. In the commuter flow, choose a one-way journey, passenger count, and departure; complete a Stripe test payment.
7. Confirm the Stripe CLI webhook marks the payment and ticket as confirmed; verify capacity and manifest totals update.
8. Start boarding, dispatch the bus, report an incident, perform a valid reassignment if needed, and verify trip history.
9. Cancel a separate confirmed booking and verify the payment/refund state and passenger ticket history.
