# UPTSLK End-to-End Test Guide

This runbook tests the complete web platform using one bidirectional service between **Kadawatha Centre** and **Makumbura Centre**. Complete each section in order; later steps rely on the records created earlier.

## 1. Before you begin

Start PostgreSQL, the API, and the React application in separate terminals.

```bash
docker compose up -d
cd apps/api && dotnet run
cd apps/web && pnpm dev
```

Open the API Swagger page at `http://localhost:5250/swagger` only when you need to inspect an API response. Use the web portal for the actual test steps.

Use the bootstrap administrator from `apps/api/appsettings.Development.json`, normally `admin@upts.lk` with password `admin123`, unless you changed it locally.

## 2. Test data overview

Create the following records. Keep the codes/names exactly as written so screenshots and evidence are easy to explain.

### Centres

| Field | Kadawatha terminal | Makumbura terminal |
| --- | --- | --- |
| Code | `KAD` | `MKB` |
| Name | `Kadawatha Centre` | `Makumbura Centre` |
| City | `Kadawatha` | `Makumbura` |
| District | `Gampaha` | `Colombo` |
| Status | `Operating` | `Operating` |

### Bays

| Centre | Code | Name | Status |
| --- | --- | --- | --- |
| Kadawatha Centre | `KAD-B01` | `Kadawatha Express Bay 1` | `Available` |
| Kadawatha Centre | `KAD-B02` | `Kadawatha Recovery Bay` | `Available` |
| Makumbura Centre | `MKB-B01` | `Makumbura Express Bay 1` | `Available` |
| Makumbura Centre | `MKB-B02` | `Makumbura Recovery Bay` | `Available` |

### Route

| Field | Value |
| --- | --- |
| Route number | `EX-KM-01` |
| Route name | `Kadawatha - Makumbura Express` |
| Owning centre | `Kadawatha Centre` |
| Service type | `AC express` or `Semi-luxury` |
| Distance | `32 km` |
| Estimated duration | `45 minutes` |
| Status | `Active` |

The route has two directions. They are different journeys under the same parent route:

| Direction | Start centre | End centre | Duration | Active |
| --- | --- | --- | --- | --- |
| Outbound | `Kadawatha Centre` | `Makumbura Centre` | `45 minutes` | Yes |
| Return | `Makumbura Centre` | `Kadawatha Centre` | `45 minutes` | Yes |

### Stops

Add these ordered stops to the relevant direction. You may use the terminal as the first/last stop if the form supports it.

| Outbound sequence | Stop |
| --- | --- |
| `1` | `Kadawatha Centre` |
| `2` | `Kaduwela Interchange` |
| `3` | `Koswatta` |
| `4` | `Makumbura Centre` |

For the return direction, add the same stops in reverse order: `Makumbura Centre`, `Koswatta`, `Kaduwela Interchange`, `Kadawatha Centre`.

### Buses

All three buses are intentionally assigned to `Kadawatha Centre`. This matches the current route-owner model while allowing the same fleet to run the outbound and return direction.

| Plate number | Model | Type | Passenger capacity | Status | Use |
| --- | --- | --- | --- | --- | --- |
| `WP-CAB-4101` | `Ashok Leyland Viking` | `Semi-luxury` | `45` | `Active` | Main scheduled bus |
| `WP-CAB-4102` | `Ashok Leyland Viking` | `Semi-luxury` | `45` | `Active` | Recovery replacement bus |
| `WP-CAB-4103` | `Tata Starbus` | `Normal` | `35` | `Active` | Capacity-warning example |

Create one completed inspection/maintenance record for `WP-CAB-4102`. Do not create an in-progress maintenance record for the replacement bus.

### Drivers

| Full name | Licence number | Centre | Status | Use |
| --- | --- | --- | --- | --- |
| `Kasun Perera` | `B-DR-4101` | `Kadawatha Centre` | `Active` | Main driver |
| `Nimal Silva` | `B-DR-4102` | `Kadawatha Centre` | `Active` | Recovery driver |
| `Ayesha Fernando` | `B-DR-4103` | `Kadawatha Centre` | `Active` | Return/duty-roster driver |

### Fare rules

Create the fare rules from the Route/Fares page. Use an amount above `LKR 2,000` so Stripe is clearly tested.

| Passenger category | Fare | Status |
| --- | --- | --- |
| `Adult` | `LKR 2,500.00` | `Active` |
| `Student` | `LKR 2,200.00` | `Active` |

## 3. Administrator setup test

Sign in as the administrator.

1. Open **Admin → Centres** and create `Kadawatha Centre`, then `Makumbura Centre`.
2. Open each centre profile and create the listed bays.
3. Open **Network → Routes**, create `EX-KM-01`, then add its two directions.
4. Add the ordered stops for both directions.
5. Open **Fleet → Vehicles**, create the three buses with the listed capacities.
6. Open **Fleet → Drivers**, create the three drivers.
7. Open **Fleet → Maintenance**, add the completed inspection for `WP-CAB-4102`.
8. Refresh the browser after each major area and confirm all records persist.

Expected result: both centres, bays, route directions, stops, fleet records, and drivers remain visible after refresh.

## 4. Centre Manager timetable and dispatch test

Sign in as a Centre Manager account associated with `Kadawatha Centre`.

### Create recurring timetables

Open **Network → Timetables** and create two timetables.

| Timetable | Direction | Departure bay | First departure | Last departure | Frequency | Days |
| --- | --- | --- | --- | --- | --- | --- |
| Outbound service | Kadawatha → Makumbura | `KAD-B01` | `06:00 AM` | `08:00 AM` | `30 minutes` | Every day |
| Return service | Makumbura → Kadawatha | `MKB-B01` | `06:00 AM` | `08:00 AM` | `30 minutes` | Every day |

Generate daily trips for tomorrow, not today. This avoids a departure time already being in the past.

Expected result: each timetable generates departures at `06:00`, `06:30`, `07:00`, `07:30`, and `08:00` for the selected service date.

### Assign dispatch resources

Open **Operations → Dispatch**.

1. Open the first outbound `06:00 AM` trip.
2. Assign `WP-CAB-4101`, `Kasun Perera`, and `KAD-B01`.
3. Open the first return `06:00 AM` trip.
4. Assign `WP-CAB-4103`, `Ayesha Fernando`, and `MKB-B01`.
5. Open **Operations → Duty roster** and confirm Kasun and Ayesha have their assigned departures.
6. Move the outbound trip from `Scheduled` to `Ready`.

Expected result: the trips appear in the correct Dispatch board columns, the Bay Management view shows the related assignments, and the duty roster groups trips by driver.

## 5. Commuter and Stripe booking test

Create a commuter account through the public **Sign up** flow. Ensure its passenger category is `Adult` if your profile/category setup asks for it.

Start Stripe CLI forwarding in a terminal before paying:

```bash
stripe listen --events checkout.session.completed,payment_intent.payment_failed,checkout.session.expired,charge.refunded --forward-to http://localhost:5250/api/v1/payments/stripe/webhook
```

Copy the displayed webhook signing secret into the local API configuration as `Payments:Stripe:WebhookSecret`, then restart the API if you changed configuration.

As the commuter:

1. Open the home booking page.
2. Enter boarding point `Kadawatha`.
3. Enter drop-off point `Makumbura`.
4. Choose tomorrow's date.
5. Set passenger count to `1` for the first payment.
6. Select `Kadawatha - Makumbura Express`, then select the `06:00 AM` outbound departure.
7. Confirm the page shows available spaces, not a seat map.
8. Continue to Stripe Checkout.
9. Use Stripe test card `4242 4242 4242 4242`, any future expiry date, any three-digit CVC, and any valid postal code.
10. Complete payment.

Expected result:

- Stripe redirects to the UPTSLK payment-status page.
- The Stripe CLI displays `checkout.session.completed`.
- The booking changes from `Pending` to `Confirmed`.
- **My Tickets** displays `Approved to board`, QR code, route, fare `LKR 2,500.00`, and passenger count `1`.
- The trip capacity decreases from `45` to `44` available spaces.
- **Passenger flow** shows the commuter in the trip manifest with count `1`.

### Passenger-count check

Repeat the booking using passenger count `2` on a different departure.

Expected result: Stripe total is `LKR 5,000.00`; the ticket and manifest show passenger count `2`; available capacity decreases by `2`.

## 6. Boarding and dispatch lifecycle test

As Centre Manager or Dispatcher:

1. Open the booked outbound trip from **Operations → Dispatch**.
2. Move it from `Ready` to `Boarding`.
3. Confirm the passenger ticket still says `Approved to board`.
4. Open **Passenger flow** and confirm the manifest/capacity information is visible.
5. Move the trip from `Boarding` to `Dispatched`.

Expected result: lifecycle transitions are visible on the Dispatch board, Trip Details page, and ticket/manifest context.

## 7. Agent Recovery test

Use a separate active trip, preferably the `06:30 AM` outbound departure, so the paid `06:00 AM` booking remains available as evidence.

1. Open the trip and select **Report incident**.
2. Use incident type `Breakdown`.
3. Use severity `High`.
4. Use title `Engine fault on scheduled service`.
5. Use description `Main bus cannot continue. Recovery assessment required.`
6. Save the incident.
7. Open **Operations → Recovery agents**.
8. Select the new incident and select **Run recovery agents**.

Expected result: four saved recommendations appear:

| Agent | Expected recommendation |
| --- | --- |
| Network Continuity Agent | Alternative available bay, normally `KAD-B02`, and a `15 minute` revised departure |
| Fleet Readiness Agent | `WP-CAB-4102` as the active replacement with `45` passenger capacity |
| Dispatch Recovery Agent | `Nimal Silva` as an active replacement driver |
| Passenger & Fare Impact Agent | Affected active booking count and no automatic refund recommendation |

Open the generated workflow. Confirm it has status `Awaiting approval` and shows all saved agent steps, reasons, warnings, and timings.

As a Centre Manager or Admin:

1. Add a decision note such as `Replacement bus and driver confirmed for delayed service.`
2. Select **Approve and apply recovery**.

Expected result:

- The workflow becomes `Completed`.
- The approval record shows who reviewed it and when it was applied.
- The trip uses `WP-CAB-4102`, `Nimal Silva`, `KAD-B02`, and the revised time.
- The trip status becomes `Delayed`.
- The incident becomes `Resolved`.
- The workflow detail retains all agent recommendations and the final decision as audit evidence.

### Safe-failure check

To demonstrate safety, mark `WP-CAB-4102` as `Maintenance` before starting another breakdown workflow.

Expected result: the Fleet Readiness Agent cannot produce a valid replacement. The workflow becomes `Failed`, records a clear reason, creates no actionable approval, and does not alter the trip.

Restore `WP-CAB-4102` to `Active` after this test.

## 8. Refund test

Use a confirmed Stripe booking that has not been completed.

1. Open the booking/ticket in staff **Fares → Bookings**.
2. Select **Cancel and refund**.
3. Enter reason `Commuter requested cancellation during test`.
4. Confirm cancellation.

Expected result: booking/ticket becomes `Cancelled`, refund status is recorded, the passenger ticket history updates, and the Stripe webhook or reconciliation page reflects the refund outcome when Stripe processes it.

## 9. Evidence checklist

Capture one screenshot for each item:

- Both centres and their bays
- Parent route with both directions and ordered stops
- Vehicle capacities and active replacement bus
- Driver list and duty roster
- Timetable with generated daily trips
- Dispatch board in `Ready`, `Boarding`, `Dispatched`, and `Delayed` states
- Stripe test checkout and completed payment status
- Confirmed commuter QR ticket with passenger count
- Passenger manifest/capacity view
- Incident report
- Agent Recovery workflow with all four recommendations
- Manager approval and final delayed/reassigned trip
- Safe-failure workflow with no valid replacement
- Cancelled booking/refund result

## 10. Pass criteria

The test is successful when:

- Both route directions generate and dispatch trips correctly.
- Passenger count affects the Stripe amount and remaining vehicle capacity.
- No passenger selects a seat number.
- Stripe webhook confirmation produces a confirmed QR ticket.
- Dispatch lifecycle data is visible in operations views.
- Agent recovery makes no direct change until a Centre Manager/Admin approves it.
- Approved recovery safely reassigns the trip and records an audit trail.
- A missing replacement bus produces a safe failure without changing the trip.
