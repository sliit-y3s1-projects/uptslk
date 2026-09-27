# UPTSLK mobile app - implementation brief

## Objective

Build a small, polished Flutter application for the UPTSLK final-viva
demonstration. The first completed integration milestone is real API-backed
authentication and profile retrieval. The remaining commuter and driver flows
will be converted from mock data one at a time.

The final app supports two roles:

1. Commuter: find a bus, choose a departure, create a mock booking, and view
   tickets.
2. Driver: view today's duties and update a duty's mock trip status.

The app should look credible and cohesive with the UPTSLK web platform, but it
must remain deliberately small and easy to demonstrate.

## Current authentication milestone

- Use `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, and
  `GET /api/v1/auth/me` from the existing ASP.NET Core API.
- Use the returned JWT as an in-memory bearer token for this milestone.
- A successful sign-in or registration must open **My profile**. Do not open
  the old mock commuter or driver shells yet.
- Public registration creates a commuter account only. Staff accounts remain
  administrator-managed.
- Do not show hard-coded demo credentials, demo login cards, a manual role
  picker, Firebase, a database client, maps, payment SDKs, or notifications.
- Do not persist the JWT yet. Secure storage and refresh-token handling are a
  later, deliberate session-management milestone.
- Do not build admin or centre-operations functionality. Those remain in the
  web portal.
- The `http` package is permitted for this API boundary. Keep dependencies
  otherwise minimal.

The old mock travel and duty screens are not part of this milestone. They must
be replaced rather than extended when their API integration begins.

## Project location and commands

The Flutter project is already created in apps/mobile.

Linux:

    cd apps/mobile
    flutter analyze
    flutter test
    flutter run --dart-define=UPTSLK_API_BASE_URL=http://10.0.2.2:5250

Windows PowerShell:

    Set-Location apps/mobile
    flutter analyze
    flutter test
    flutter run --dart-define=UPTSLK_API_BASE_URL=http://10.0.2.2:5250

For a physical device, replace `10.0.2.2` with the LAN IP address of the
computer running the API. For the deployed API, pass its HTTPS Choreo URL.

## Authentication behavior

The sign-in screen must contain email, password, loading, validation, API error,
and sign-up entry states. The sign-up screen must contain full name, email,
password, confirm password, and a clear explanation that it creates a commuter
account. Keep the visual treatment custom: use composed surfaces, custom field
containers, clear spacing, and a branded primary action rather than default
Material form styling.

The profile screen must show the name, email, role, profile photo when present,
home location, identity-verification status, and an in-memory sign-out action.

## Visual language

The app should feel calm, trustworthy, and transport-focused. Avoid dense
dashboards, large illustrations, gradients, glass effects, and excessive
badges.

Define colour tokens in one theme file:

| Token | Colour | Use |
| --- | --- | --- |
| Brand primary | #302B6B | Primary buttons, active navigation, headings |
| Brand light | #EEF0FF | Selected surfaces and light icon backgrounds |
| Background | #F7F8FC | Screen background |
| Surface | #FFFFFF | Cards and sheets |
| Ink | #171A2B | Main text |
| Muted | #667085 | Secondary text |
| Border | #DDE1EA | Cards and inputs |
| Success | #047857 | Ready, completed, confirmed |
| Warning | #B45309 | Boarding or attention |
| Danger | #B42318 | Cancelled or error |

Use Material 3 and platform typography. Keep screen padding at 20 px, use
8/12/16/24 px spacing, 12–16 px card radii, visible light borders, and touch
targets of at least 48 px. Use SafeArea and scrollable layouts so small Android
phones do not overflow.

## Navigation

Use a small named-route or Navigator setup. Do not add a routing package.

    Login
    ├─ Commuter shell
    │  ├─ Home / Find a bus
    │  ├─ Search results
    │  ├─ Departure details
    │  ├─ Booking success
    │  ├─ My tickets
    │  └─ Profile
    └─ Driver shell
       ├─ Today's duties
       ├─ Duty details
       └─ Profile

Bottom navigation:

- Commuter: Home, Tickets, Profile.
- Driver: Duties, Profile.

Use full pages for search results, booking confirmation, and duty details.

## Commuter flow

### 1. Login

Show the UPTSLK wordmark, “Travel made simple”, email/password fields, one
primary Sign in button, and the two demo account cards.

### 2. Home — Find a bus

Keep the booking start point obvious:

1. Greeting: “Good morning, Sahan”.
2. A Find your journey card:
   - From: Kadawatha Centre
   - To: Makumbura Centre
   - Small swap control
   - Travel date: 24 Sep 2026
   - Passenger stepper, default 1, range 1–4
   - Full-width Search buses button
3. One or two compact Popular routes cards.

Selectors do not need real data. A simple bottom sheet that switches between
Kadawatha and Makumbura is enough. The date can use Flutter's date picker.

### 3. Search results

Display Kadawatha Centre → Makumbura Centre and the route label
EX-KM-01 · Kadawatha – Makumbura Express prominently.

Show these departure cards:

| Time | Bay | Availability | Fare |
| --- | --- | --- | --- |
| 06:00 AM | KAD-B01 | 44 spaces left | LKR 180 |
| 07:00 AM | KAD-B01 | 31 spaces left | LKR 180 |
| 08:00 AM | KAD-B02 | 18 spaces left | LKR 180 |

Each card gets one understated Select action. Keep time, bay, direction,
capacity, and fare easy to scan.

### 4. Departure review and mock booking

After selection, show route/direction, selected date/time, bay code, an
editable passenger stepper, and Fare: LKR 180 × passenger count. Show the mock
payment method UPTSLK Wallet · LKR 2,500 available and one Confirm booking
button.

On confirmation, add a booking to in-memory state and show:

    Booking confirmed
    EX-KM-01 · 06:00 AM
    24 September 2026 · KAD-B01

Give the user View my ticket and Back to home buttons. There is no real
payment, seat selection, or QR scanner. A decorative boarding-pass pattern is
allowed if it is clearly labelled.

### 5. My tickets

Seed these tickets and append newly created in-memory bookings:

| Status | Route | Date/time | Bay |
| --- | --- | --- | --- |
| Upcoming | EX-KM-01 | 24 Sep 2026 · 06:00 AM | KAD-B01 |
| Completed | EX-KM-01 | 20 Sep 2026 · 08:00 AM | KAD-B02 |

The upcoming ticket can offer Cancel booking with a confirmation dialog; it
only changes local state.

### 6. Commuter profile

Show Sahan Perera, commuter@demo.upts.lk, wallet balance LKR 2,500, one local
notification toggle, and Logout. Do not add account-management forms.

## Driver flow

The driver UI is focused on the next departure, not centre management.

### 1. Today's duties

Header:

    Good morning, Nimal
    Tuesday, 24 September

Show a highlighted Next duty card, then a short Later today list:

| Time | Direction | Vehicle | Bay | Status |
| --- | --- | --- | --- | --- |
| 06:00 AM | Kadawatha → Makumbura | WP-CAB-4103 | KAD-B01 | Ready |
| 08:00 AM | Makumbura → Kadawatha | WP-CAB-4103 | MKB-B01 | Scheduled |

The next-duty card has one obvious action: Open duty.

### 2. Duty details

Display route/direction, scheduled time, bay, vehicle, passenger count 18 / 45,
and this stop list:

    Kadawatha Centre → Kelaniya → Nugegoda → Makumbura Centre

Use this local status progression:

    Scheduled → Ready → Boarding → Departed → Completed

Only show the next valid action, for example Start boarding, then Mark
departed, then Complete trip. Confirm before departure and completion. Do not
add GPS, passenger scanning, chat, incident reporting, or dispatch assignment.

### 3. Driver profile

Show Nimal Silva, Driver ID DRV-4103, Kadawatha Centre, assigned vehicle
WP-CAB-4103, and Logout.

## Models and local state

Use small typed Dart models, not copies of the full backend schema:

    AppUser: id, name, email, role
    RouteDirection: id, routeNumber, routeName, origin, destination
    Departure: id, direction, dateTime, bayCode, capacity, bookedPassengers, fare
    Booking: id, departure, passengerCount, status
    DriverDuty: id, departure, vehiclePlate, passengerCount, status, stops

Place seed data in lib/data/mock_data.dart. Use readable IDs such as
departure-0600; UUIDs add no value here.

Use one small DemoStore with ChangeNotifier or simple StatefulWidget state. It
holds the selected role, passenger count, new bookings, cancellations, and
driver duty status. Do not introduce Bloc, Redux, Riverpod, repository layers,
or persistence.

## Folder structure

Create folders only when they contain real files:

    apps/mobile/lib/
      main.dart
      core/theme/app_theme.dart
      core/config/api_config.dart
      data/mock_data.dart
      models/
        app_user.dart
        route_direction.dart
        departure.dart
        booking.dart
        driver_duty.dart
      services/auth_api_service.dart
      state/auth_store.dart
      shared/widgets/
        app_card.dart
        app_primary_button.dart
        passenger_stepper.dart
        route_summary.dart
        section_header.dart
        status_chip.dart
      features/
        auth/
          auth_widgets.dart
          login_page.dart
          register_page.dart
        profile/profile_page.dart
        commuter/
          commuter_shell.dart
          commuter_home_page.dart
          search_results_page.dart
          departure_details_page.dart
          booking_success_page.dart
          tickets_page.dart
          commuter_profile_page.dart
        driver/
          driver_shell.dart
          duties_page.dart
          duty_details_page.dart
          driver_profile_page.dart

Reusable widget responsibilities:

- AppCard: standard surface, border, radius, and padding.
- StatusChip: consistent status colours.
- PassengerStepper: minus/count/plus UI with callbacks.
- RouteSummary: origin → destination and route metadata.

Do not turn every text row into a widget. Extract one only when it is reused,
clarifies a page, or owns a small interaction.

## Build order

1. Theme, API configuration, user model, auth API service, and auth store.
2. Sign-in, registration, loading, validation, and failure states.
3. Profile retrieval and in-memory sign-out.
4. Replace commuter journey search with API-backed route and trip data.
5. Replace booking and tickets with API-backed capacity and payment flows.
6. Replace driver duties and status progression with API-backed operations.
7. Add secure session storage and refresh handling after API flows are stable.

## Definition of done

- [ ] flutter analyze has no errors.
- [ ] A real existing account can sign in through the ASP.NET API.
- [ ] A new commuter can register through the ASP.NET API.
- [ ] The authenticated profile is loaded using `GET /api/v1/auth/me`.
- [ ] Sign-out clears the in-memory session and returns to sign-in.
- [ ] No hard-coded demo credentials remain in the reachable application.
- [ ] Screens do not overflow on a typical Android phone.
- [ ] The auth experience uses custom composed controls rather than default
      Material-looking form elements.

## Final instruction

Implement the smallest complete flow first. When uncertain, choose one calm
screen with one clear primary action over a dense dashboard or an extra feature.

    User signs in or registers → profile is retrieved → user signs out.

Everything beyond this is a future phase after backend integration is
explicitly requested.
