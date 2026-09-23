# UPTSLK Mobile App Flow

This document defines the first practical Flutter release. The app has two roles: **Passenger** and **Driver**. One signed-in account receives one role from the API; the app must not show a manual role picker.

## Shared rules

- Use the existing UPTSLK colours, typography, icons, and thin-border card style.
- Keep screens simple, responsive, and usable with one hand. Avoid charts, decorative dashboards, and complex workflows.
- Store only the authenticated session provided by the API. Send requests with the access/refresh cookie or mobile session mechanism agreed with the API team.
- Show loading, empty, validation, success, and network-error states on every API-backed screen.
- Hide actions the current role cannot perform; the API remains the authority.

## Entry flow

```text
Splash → restore session →
  signed out: Sign in → Passenger registration (passengers only)
  Passenger: Passenger home
  Driver: Driver home
```

Sign out clears the session and returns to Sign in. If a session expires, show a short message and return to Sign in.

## Passenger flow

Passenger bottom navigation: **Home · Trips · Wallet · Profile**.

### 1. Passenger home

Show a greeting, current wallet balance, a prominent **Find a trip** action, and the next upcoming ticket. If there are no bookings, show a simple empty state.

### 2. Find a trip

Fields: origin centre, destination, travel date, and passenger count. Submit to load matching trips. Keep the map as a small optional preview, never the main interaction.

API: `GET /api/v1/routes`, `GET /api/v1/trips?routeId={id}&date={date}`.

### 3. Trip results and details

Results show route, departure time, bay, vehicle, status, fare, and remaining seats. Selecting a result opens trip details with stops and a **Select seat** action.

API: `GET /api/v1/trips/{tripId}`, `GET /api/v1/fare-rules/quote?tripId={tripId}&passengerId={passengerId}`.

### 4. Seat selection

Show a simple bus layout with available, occupied, and selected states. Prevent selecting unavailable seats. Continue to a confirmation screen with the selected seat, fare, and wallet balance.

API: `GET /api/v1/bookings/trips/{tripId}/seats`.

### 5. Confirm and pay

Show trip, seat, passenger count, fare, and balance after payment. Confirm using the authenticated passenger endpoint; never ask the user for a passenger UUID.

API: `POST /api/v1/bookings/me` with `tripId` and `seatNumber`.

Show the booking reference and QR ticket after success. Display API errors such as insufficient balance or an already-booked seat clearly.

### 6. Tickets, wallet, and profile

- **Trips:** Upcoming and History tabs. Open a ticket, change an eligible seat, or cancel before boarding.
- **Wallet:** Balance, top-up amount, and recent fare/refund transactions.
- **Profile:** Name, email, phone, home location, NIC verification state, password, and sign out.

APIs: booking list/detail/cancel/seat endpoints, passenger wallet top-up endpoint, and `/api/v1/auth/me`, `/api/v1/auth/me/verify-nic`, `/api/v1/auth/change-password`.

## Driver flow

Driver navigation should stay intentionally small: **Assignments · Profile**. Do not include fleet administration, route creation, employee management, or reports.

### 1. Driver home / assignments

Show today’s assigned trips in time order. Each row contains time, route, bay, vehicle, passenger count, and status. Highlight the next assignment.

API: `GET /api/v1/trips?driverId={driverId}&date={date}`.

### 2. Trip operation

Open a trip to see the route, stops, vehicle, bay, notes, passenger count, and lifecycle:

```text
Scheduled → Ready → Boarding → Dispatched → Completed
```

Only show the next valid transition. Include **Report delay or incident** as a secondary action.

API: `GET /api/v1/trips/{tripId}`, `PATCH /api/v1/trips/{tripId}/status`.

### 3. Passenger manifest

Show seat number, passenger name, booking reference/QR indicator, and boarding state. Keep it as a compact searchable list for use at the gate.

API: `GET /api/v1/bookings/trips/{tripId}/manifest`.

### 4. Delay and incident reporting

Delay: choose 5, 10, 15, or 20+ minutes and optionally add a note. Incident: trip is preselected; choose type and severity, enter a short title and description, then submit.

API: use the trip status endpoint for supported delay/status changes and `POST /api/v1/incidents` for incidents.

## Delivery order

1. Flutter project shell, session restore, sign in, and role-based routing.
2. Passenger search, trip results, details, seats, and authenticated booking.
3. Passenger tickets, wallet, profile, and cancellation/seat change.
4. Driver assignments, trip lifecycle, manifest, delay, and incident report.
5. Offline/error states, accessibility checks, and device testing.

## Definition of done

Passenger can register, sign in, find a real trip, select an available seat, pay from the wallet, and view the ticket. Driver can sign in, see only assigned trips, progress a trip through valid states, view the manifest, and report an incident. All data comes from the API; no production screen depends on mock records.
