# UPTSLK Integration and UX Remediation Plan

## Progress

| Item | Status |
| --- | --- |
| Booking API validation and readable client errors | Done |
| Bay creation in a dialog | Done |
| Centre dashboard trips, loading, and empty states | Done |
| Live seat occupancy and passenger booking completion | In progress |
| Assistance and support request lifecycle API/UI | In progress |
| Passenger account creation with optional portal credentials | Done |
| Passenger account restore and password reset | Done |
| Passenger account management improvements | In progress |

This plan covers the remaining issues in the Centre Operations portal, public passenger booking flow, and passenger-facing operations screens. The goal is to make every visible action backed by a real API response and provide a transport-appropriate interface.

## Current problems

| Area | Problem | Result |
| --- | --- | --- |
| Centre operations | Some operational screens still have incomplete states or weak API feedback | Centre staff cannot reliably complete daily work |
| Bay management | Bay creation is presented in-page instead of a focused dialog | Form is difficult to scan and interrupts the management list |
| Public booking | Passenger can choose a route but cannot see a useful bus seat layout | Booking flow stops before a credible seat choice |
| Booking confirmation | Confirm booking returns “Request failed” | Passenger cannot complete a real booking |
| Passenger flow | Flow screen is mostly presentation data | Staff cannot inspect or act on live passenger movement |
| Assistance | Assistance screen is not connected to a complete operational workflow | Requests cannot be tracked from open to resolved |
| Accounts | Passenger accounts view is not a complete account-management experience | Staff cannot search, inspect, or manage passenger records properly |
| Support | Support screen lacks a real ticket lifecycle | Requests cannot be triaged, assigned, responded to, or closed |
| Transport UX | Several screens use generic dashboard presentation | Important route, bay, vehicle, passenger, and status information is hard to scan |

## Phase 0 — Reproduce and classify failures

Before changing UI, verify each issue against the running API and PostgreSQL database.

1. Sign in as a commuter, search for a real route, select a real departure, choose a seat, and capture the exact response from `POST /api/v1/bookings/me`.
2. Confirm the selected trip has an active fare rule for the passenger category and that the passenger wallet has enough balance.
3. Confirm the trip status is bookable and the selected seat is still available.
4. Sign in as a Centre Manager and test bay create, update, availability, and close operations after a page refresh.
5. Record every failing request, response status, validation message, and missing API endpoint in a short issue list.

This phase must distinguish a frontend payload problem from a missing fare, wallet, trip, migration, or authorization problem.

## Phase 1 — Stabilize booking and seat selection

This is the highest-priority user journey.

### Backend

- Return structured booking errors for missing passenger profiles, missing fare rules, insufficient balance, unavailable trips, invalid seats, and seat conflicts.
- Add an authenticated passenger booking read endpoint so a commuter can list only their own bookings.
- Confirm the seat endpoint returns capacity, seat number, availability, and any accessibility/reserved state required by the UI.
- Ensure booking, wallet debit, and transaction creation remain one database transaction.
- Add a development-safe way to create or top up a commuter wallet for testing without seeding production data.

### Frontend

- Replace the placeholder seat grid with a recognisable bus layout: driver area, aisle, paired seats, row numbers, occupied seats, available seats, and selected seat.
- Load seats after a departure is selected and refresh availability before confirmation.
- Show fare, wallet balance, booking total, and remaining balance before payment.
- Display the API’s actual error message beside the confirm action.
- On success, show booking reference, QR ticket, route, bay, vehicle, date/time, and seat.
- Add loading, empty, conflict, insufficient-balance, and session-expired states.

## Phase 2 — Improve Centre Operations

### Bay management

- Move bay creation into a dialog opened by **Create bay**.
- Keep the list visible behind the dialog and refresh it after creation/update/close.
- Validate code, name, status, and centre before submitting.
- Use a confirmation dialog for closing a bay and explain that it is archived rather than physically deleted.
- Show API validation and conflict errors in the dialog.

### Centre dashboard

- Replace remaining mock or empty operational metrics with API-backed values.
- Show active bays, current boarding trips, next departures, incidents, and capacity using real records.
- Use explicit loading and empty states instead of placeholder numbers.
- Make route, trip, bay, and incident rows link to their corresponding detail screens.
- Keep centre scope enforced by both the API and the UI.

## Phase 3 — Passenger operations screens

Each screen needs a real list/detail/action lifecycle, not only cards or static summaries.

### Passenger flow

- Show live trips, boarding state, passenger count, capacity, and bay.
- Add filters for centre, route, time, and status.
- Open a trip passenger manifest and show booked, boarded, and missing passengers.
- Add clear empty and delayed-service states.

### Assistance

- Define an assistance record with passenger, trip/booking, category, priority, description, status, assigned staff member, and timestamps.
- Add create, assign, update, resolve, and reopen actions.
- Use a queue layout with priority and SLA-friendly status indicators.

### Accounts

- Keep passenger account records separate from staff Employees.
- Add search by name, email, phone, passenger ID, and status.
- Show profile, category, wallet balance, booking history, and account status.
- Allow only authorized account actions such as disable/restore and support handoff.

### Support

- Define a support ticket lifecycle: Open → In progress → Waiting for passenger → Resolved → Closed.
- Add subject, category, priority, passenger, booking/trip reference, conversation notes, assignee, and timestamps.
- Provide list filters, detail view, internal notes, response state, and close/reopen actions.

## Phase 4 — Transport-level UI system

- Use consistent dense list/table layouts for operational work.
- Reserve cards for summaries and detail sections, not every row.
- Use the same status vocabulary everywhere: Scheduled, Ready, Boarding, Dispatched, Completed, Delayed, Cancelled, Open, Resolved, Closed.
- Use route and trip identifiers with human-readable names; never show raw UUIDs as primary labels.
- Make bay, route, vehicle, driver, passenger, and booking relationships visible in context.
- Add keyboard/focus states, disabled states, confirmation states, and responsive layouts.
- Remove mock values from production views; show a meaningful empty state when the API has no data.

## Phase 5 — Verification

Run the following scenarios against a disposable database:

1. Admin creates a centre, bay, route, schedule, vehicle, driver, and fare rule.
2. Dispatcher creates a trip and moves it through valid lifecycle states.
3. Commuter registers, receives a passenger profile and wallet, tops up, searches, selects a trip, selects a seat, and confirms a booking.
4. A second commuter attempts the same seat and receives a clear conflict message.
5. A commuter with insufficient balance receives a clear payment error and no booking is created.
6. Staff inspect passenger flow, assistance, accounts, and support records and complete their lifecycle actions.
7. Refresh every screen and confirm persisted API data remains visible.
8. Disable an account, verify login is rejected with the disabled-account message, then restore it and verify login works again.

## Completion criteria

The remediation is complete when the booking journey succeeds against real persisted data, seat conflicts and payment failures are understandable, bay creation uses a dialog, Centre Operations shows no fake operational values, and Passenger Flow, Assistance, Accounts, and Support each have working API-backed list/detail/action workflows.
