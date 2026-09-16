# UPTSLK end-to-end runbook

Run the scenarios in order against a disposable development database. All records below use the `E2E-` prefix so they are easy to identify. This runbook uses the real API and the real React portals; no demo login or seeded operational records are required.

## 1. Start the system

From the repository root:

```bash
docker compose up -d
cd apps/api
dotnet ef database update
dotnet run
```

In another terminal:

```bash
cd apps/web
cp .env.example .env
pnpm install
pnpm dev
```

Open `http://localhost:5173`. The bootstrap Super Admin is created by the API on startup:

```text
Email: admin@upts.lk
Password: admin123
```

## 2. Super Admin portal: create the organisation

Sign in with the bootstrap account. You should land in the **Super Admin portal** at `/admin`.

### Create a centre

Go to **Super Admin → Multimodal centres → Create centre**.

Enter:

```text
Code: E2E-COLOMBO-01
Name: E2E Test MMC
City: Colombo
District: Colombo
Description: Temporary end-to-end centre
Status: Operating
```

Save the centre. Confirm it appears in **Super Admin → Multimodal centres** and open its profile. Copy the UUID from the browser URL; this is `CENTRE_ID` for later steps.

### Create a centre employee

Go to **Super Admin → Employees → Add employee**. Complete the dialog:

```text
Full name: E2E Centre Manager
Work email: manager.e2e@upts.lk
Temporary password: use Generate or enter Manager123!
Role: CentreManager
Assigned centre: E2E Test MMC
```

Select **Create account**. Use **Copy credentials** or **Share by email** to verify the local sharing UI. Sign out from the Super Admin portal.

## 3. Centre Manager portal: centre operations

Sign in with `manager.e2e@upts.lk` and the temporary password. The user should enter the **Centre Manager portal**, scoped to E2E Test MMC.

### Create a bay

Go to **Operations → Bay management** (`/operations/bays`) and select **Create bay**.

```text
Code: E2E-B01
Name: Verification Bay
Status: Available
```

Confirm the bay appears under E2E Test MMC. Open it and test the available status actions. Refresh and confirm persistence.

### Create a route

Go to **Network → Routes → Create route** (`/network/routes/new`).

```text
Route number: E2E-177
Name: E2E Centre - Kaduwela
Origin: E2E Test MMC
Destination: Kaduwela
Service type: Normal
Distance: 18.5 km
Estimated duration: 45 minutes
```

Add ordered stops:

```text
1. E2E Test MMC — 6.80, 79.92
2. Gonahena — 6.90, 79.95
3. Kaduwela — 6.93, 80.00
```

Open the route detail, verify the stop order, edit the route, and return to the route list. Confirm the updated values remain after refresh.

### Add a recurring timetable

Go to **Network → Timetables**, select E2E-177, and add:

```text
First departure: 06:00
Last departure: 22:00
Headway: 30 minutes
Operating days: Monday through Sunday
Bay: Verification Bay
```

Confirm the timetable appears after refresh.

## 4. Fleet and maintenance

Go to **Fleet → Vehicles → Register vehicle**.

```text
Centre: E2E Test MMC
Registration: E2E-7714
Model: E2E Test Bus
Type: Normal
Capacity: 40
Accessible: Yes
Status: Active
```

Open the vehicle profile, edit the model or capacity, save, and verify the updated card. Then go to **Fleet → Drivers → Add driver**:

```text
Full name: E2E Test Driver
Phone: 0770000000
Licence number: E2E-LIC-20260907
Status: Active
Centre: E2E Test MMC
```

Finally go to **Fleet → Maintenance → Add record** and create an inspection for E2E-7714. Open the record, edit it, and verify it after refresh.

## 5. Scheduling and dispatch

Go to **Operations → Dispatch → Create trip**.

```text
Route: E2E-177
Service date: tomorrow
Scheduled time: 11:00
Bay: E2E-B01
Vehicle: E2E-7714
Driver: E2E Test Driver
Notes: Recurring 30-minute service
```

Open the trip detail and verify the lifecycle. Test the transitions in order:

```text
Scheduled → Ready → Boarding → Dispatched → Completed
```

Also test reassignment to another available vehicle or driver, then cancel a separate test trip and confirm the cancelled status remains after refresh.

### Incident workflow

Go to **Operations → Incidents → Report incident**. Use:

```text
Type: Vehicle issue
Description: E2E test incident
Vehicle: E2E-7714
Trip: the E2E-177 trip
Severity: Medium
```

Open the incident, update its status, resolve it, and verify the resolution in the incident list.

## 6. Passengers, bookings, and fares

Go to **Passengers → Rider accounts → Add passenger**:

```text
Full name: E2E Passenger
Phone: 0772090701
Email: e2e-passenger@example.test
Category: Student
```

Create a fare rule under **Fares & finance → Fare rules** for E2E-177, then create a booking under **Passengers → Passenger flow** for the E2E passenger. Select an available seat, confirm the fare, and verify the booking in **Tickets** and the passenger manifest. Test cancellation/refund if those actions are enabled.

## 7. Super Admin verification and archive behavior

Sign back in as `admin@upts.lk` and confirm:

- **Super Admin → Overview** shows only API centres and API employees.
- **Super Admin → Employees** lists the E2E Centre Manager.
- **Super Admin → Roles & permissions** shows the platform roles.
- **Super Admin → Multimodal centres** opens the E2E centre profile.

On the centre profile, select **Delete centre**, confirm in the alert dialog, and verify the centre changes to `Closed`. Closed centres are hidden from the normal list. Archived records remain available through `GET /api/v1/centres?includeClosed=true`.

## 8. Acceptance checklist

For every create, read, update, and close/cancel operation, verify the result in the UI, refresh the page, and confirm the API-backed value remains. Record failures with the route, account role, request data, response status, and screenshot. Do not use display codes in API paths where a UUID is required.
