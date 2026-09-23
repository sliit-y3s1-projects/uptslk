# UPTSLK mobile app — AI implementation brief

## Objective

Build a small, polished Flutter UI prototype for the UPTSLK final-viva
demonstration. It supports two roles:

1. Commuter: find a bus, choose a departure, create a mock booking, and view
   tickets.
2. Driver: view today's duties and update a duty's mock trip status.

The app should look credible and cohesive with the UPTSLK web platform, but it
must remain deliberately small and easy to demonstrate.

## Strict scope

- Build only a Flutter UI with local mock data and in-memory state.
- Do not integrate the ASP.NET API, Choreo, Neon, Stripe, or any database.
- Do not add HTTP clients, Firebase, secure storage, real authentication,
  maps, push notifications, QR scanning, or payment packages.
- Do not use real credentials, tokens, API URLs, or secrets.
- Do not build admin or centre-operations functionality. Those remain in the
  web portal.
- Do not add a package unless it creates a clear visible benefit. Prefer the
  Flutter SDK, Material 3 widgets, and Material icons.

Restarting the app may reset mock bookings and duty statuses. That is expected.

## Project location and commands

The Flutter project is already created in apps/mobile.

Linux:

    cd apps/mobile
    flutter analyze
    flutter test
    flutter run

Windows PowerShell:

    Set-Location apps/mobile
    flutter analyze
    flutter test
    flutter run

## Local demo login

Show these credentials below the login form in compact Demo access cards. They
are UI-only values and must not call a backend.

| Role | Email | Password | Landing screen |
| --- | --- | --- | --- |
| Commuter | commuter@demo.upts.lk | Demo12345 | Find a bus |
| Driver | nimal.driver@demo.upts.lk | Demo12345 | Today's duties |

Rules:

- Tapping a demo card may prefill its email and password.
- The user must still press Sign in.
- Accept only these two combinations.
- For another combination, show: “Use one of the demo accounts shown below.”
- Logout returns to login and clears in-memory session state.

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
      app.dart
      core/theme/app_theme.dart
      core/constants/demo_accounts.dart
      data/mock_data.dart
      models/
        app_user.dart
        route_direction.dart
        departure.dart
        booking.dart
        driver_duty.dart
      state/demo_store.dart
      shared/widgets/
        app_card.dart
        app_primary_button.dart
        passenger_stepper.dart
        route_summary.dart
        section_header.dart
        status_chip.dart
      features/
        auth/login_page.dart
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

1. Theme, app shell, models, mock data, and demo store.
2. Login and role-based navigation.
3. Commuter search, results, review, and local booking.
4. Tickets and commuter profile.
5. Driver duties, details, and status progression.
6. Driver profile and logout.
7. Run analysis/tests and test both accounts on an Android-sized emulator.

## Definition of done

- [ ] flutter analyze has no errors.
- [ ] Both listed demo accounts sign in locally.
- [ ] A commuter can search, select a departure, change passenger count,
      confirm a booking, and see it in My Tickets.
- [ ] A driver can open a duty and progress its local status.
- [ ] Logout works for both roles.
- [ ] No application feature calls a network, database, API, or payment service.
- [ ] Screens do not overflow on a typical Android phone.
- [ ] Colours, cards, spacing, buttons, and status chips are consistent.

## Final instruction

Implement the smallest complete flow first. When uncertain, choose one calm
screen with one clear primary action over a dense dashboard or an extra feature.

    Commuter signs in → finds a bus → confirms a booking → sees a ticket.
    Driver signs in → sees today’s duty → starts and completes the duty.

Everything beyond this is a future phase after backend integration is
explicitly requested.
