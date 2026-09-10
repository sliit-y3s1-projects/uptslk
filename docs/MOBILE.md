# UPTSLK Mobile Application

## Purpose

The Flutter app is the mobile companion to the UPTSLK web console. It supports people travelling through a multimodal centre and drivers delivering scheduled bus services. Centre management, fleet administration, network design, employee administration, and governance remain in the React web application.

## Users

### Passenger

Passengers use the app to discover a service, reserve a seat, pay from their wallet, and carry a digital ticket.

- Register, sign in, manage profile, and view wallet balance.
- Browse available trips by centre, route, and travel date.
- Review route stops, departure time, vehicle, and service state.
- Request a fare quote based on passenger category.
- Select an available seat and confirm a booking.
- Top up a wallet and receive booking charges or cancellation refunds.
- View a QR-based ticket, upcoming bookings, and booking history.
- Change a seat or cancel an eligible booking before boarding.
- Receive booking, delay, cancellation, departure, and refund notifications.

### Driver

Drivers use the app for their assigned work only. They do not manage fleet records or routes.

- Sign in and view today’s and upcoming assigned trips.
- Review route, vehicle, bay, scheduled time, notes, and passenger count.
- View the passenger manifest during boarding.
- Progress a trip through allowed states: `Ready`, `Boarding`, `Dispatched`, and `Completed`.
- Record a delay when needed.
- Report a trip-linked incident with type, severity, title, and description.

## Navigation

```text
Login
├── Passenger home
│   ├── Find trip
│   ├── Trip details and seat map
│   ├── My tickets
│   ├── Wallet
│   └── Profile
└── Driver home
    ├── My assignments
    ├── Trip operation
    ├── Passenger manifest
    ├── Report incident
    └── Profile
```

The logged-in role determines which home and navigation structure is visible.

## Key Passenger Workflow

1. Select a centre, route, and departure.
2. View trip details and a live seat map.
3. Request a fare quote for the passenger category.
4. Choose a seat and confirm payment from the wallet.
5. Display the confirmed QR ticket.
6. Retain the ticket in booking history; allow an eligible cancellation and show the refunded amount.

## Key Driver Workflow

1. Open an assigned trip.
2. Review vehicle, bay, route, and manifest.
3. Mark the trip ready, open boarding, dispatch, and complete it as service progresses.
4. Report a delay or incident if operational support is required.

## API Dependencies

The Flutter app will consume existing ASP.NET Core endpoints for passengers, wallets, fare rules/quotes, bookings, seats, manifests, trips, and incidents. Authentication and role-based API authorization must be completed before production integration.

## Out of Scope

The mobile app must not duplicate web-console management functions: creating centres/routes, registering vehicles, managing drivers, maintenance, employee roles, platform governance, or Super Admin workflows.

## Google Stitch Design Prompt

Copy the following prompt into Google Stitch to create the complete Flutter mobile design system and screens.

```text
Design a production-quality mobile application named “UPTSLK” for Sri Lanka’s multimodal public transport service. It is a role-based Flutter app with two clearly separate experiences: Passenger and Driver. Create every screen listed below as a cohesive mobile product, not a landing page or a collection of generic dashboard cards.

Visual direction
- Match the UPTSLK web console: deep transport red primary (#D0001B / close to the existing console red), white surfaces, very light warm-grey page background, near-black text, muted grey secondary text, thin light-grey borders, green success, amber warning, and red destructive states.
- Use Inter or a similarly clean modern sans-serif. Use 390 × 844 mobile frames.
- Clean public-service software aesthetic: practical, compact, calm, trustworthy, and accessible.
- No gradients, glassmorphism, floating decorative blobs, oversized hero sections, stock photos, excessive rounded cards, or visual noise.
- Do not use drop shadows. Use 1px borders, spacing, grouping, and subtle background contrast for hierarchy.
- Use rounded corners sparingly (8px maximum), clear labels, realistic icons, large tap targets, and readable contrast.
- Use bottom navigation only where it improves frequent mobile actions. Keep Driver navigation especially simple.
- Use a small UPTSLK wordmark in the header, not a large logo treatment.

Realistic shared mock data
- Current centre: Kadawatha MMC, Gampaha District.
- Route 177: Kadawatha MMC → Kaduwela. Bay B04. Every 30 minutes. 12 stops. Estimated 32 minutes.
- Next departure: 11:00, vehicle WP ND-7714, Ashok Leyland Viking, driver D. Kumara, 44 seats.
- Route EX04: Kadawatha → Kandy. Bay B02. Next departure 11:30.
- Passenger: Nethmi Jayasinghe, Student category, wallet balance Rs. 1,250.
- Student fare for Route 177: Rs. 120.
- Driver: D. Kumara, licence DRV-078.
- Example service state: Boarding. Occupancy: 62%.

Create a shared screen set
1. Splash / session restore: small UPTSLK wordmark, clean loading state.
2. Sign in: email, password, sign-in button, clear validation state, and a quiet role-aware message. Include a simple registration entry point for passengers.
3. Role entry / account state: show how Passenger and Driver are routed to different home experiences after login. This is a utility screen, not a marketing role picker.
4. Notification list: booking confirmation, departure reminder, 8-minute delay, cancellation refund. Use compact chronological rows with status dots and no decorative cards.
5. Profile: name, phone, email, role/category, and account actions.

Create the Passenger experience
1. Passenger home with a compact greeting, wallet balance, “Find a trip” as the primary action, and a small upcoming-ticket section. Use bottom navigation: Home, Trips, Wallet, Profile.
2. Find a trip: origin centre selector set to Kadawatha MMC, destination/search input set to Kaduwela, travel date, and search button. Include a small map preview with a simple route line; do not make the map dominate the page.
3. Departure results: clear list rows, not large cards. Show Route 177, destination, departure times 10:30, 11:00, 11:30, bay B04, vehicle when assigned, service status, and seat availability. Include EX04 to Kandy as a secondary result.
4. Route 177 trip details: timeline of stops, departure 11:00, Bay B04, WP ND-7714, Boarding status, fare from Rs. 120, and a compact embedded map with Kadawatha-to-Kaduwela route polyline. Primary button: Select seat.
5. Seat selection: a vertical, recognisable bus interior. Driver cabin at the front, entry door, central aisle, paired seats, row numbers, and emergency exit. Show 44 seats. Use red for occupied, amber for reserved, white with border for available, and blue for accessible. Make seat 18 selected. Do not make this look like a phone grid or a generic rounded card.
6. Booking confirmation: selected Seat 18, Route 177, 11:00, fare Rs. 120, current wallet balance Rs. 1,250, balance after payment Rs. 1,130. Include a clear confirm-and-pay button and a small terms/cancellation note.
7. Digital ticket: QR code, booking reference BKG-9021, route, date/time, Bay B04, vehicle, Seat 18, passenger name, and Boarding status. This is the most important passenger screen: make it clean, scannable, and credible.
8. My tickets: segmented Upcoming / History views. Show an active Route 177 ticket, one completed ticket, and one cancelled ticket with refund Rs. 120. Include clear seat-change and cancel actions only for eligible bookings.
9. Wallet: balance Rs. 1,250, Top up action, amount selector, recent transactions: wallet top-up +Rs. 1,000, Route 177 fare -Rs. 120, cancellation refund +Rs. 120. Make this transactional and simple, not a banking app clone.

Create the Driver experience
1. Driver home: extremely simple. Header shows “Good morning, D. Kumara”. The main content is today’s assigned trips, led by Route 177 at 11:00, Bay B04, WP ND-7714, status Boarding. Show one secondary upcoming assignment. Use bottom navigation: Assignments, Profile. Include a visible “Report incident” action from the assignment detail, not as a permanent complex navigation item.
2. My assignments: concise chronological list of today’s trips. Each row shows time, route/destination, bay, vehicle, and status. No charts, performance metrics, or unnecessary cards.
3. Driver trip operation: Route 177 at 11:00. Show route, bay B04, vehicle WP ND-7714, passenger count, and a horizontal lifecycle: Scheduled → Ready → Boarding → Dispatched → Completed. Boarding is current. Use red completed states, a clear active state, and muted pending states. Primary context action is “Dispatch trip”; secondary action is “Report delay or incident”.
4. Passenger manifest: compact list with seat number, passenger name, booking reference/QR indicator, and boarding state. Include summary “27 of 44 passengers confirmed”. Keep scanning use-cases in mind.
5. Incident report: trip preselected, issue type selector (Delay, Breakdown, Safety, Other), severity (Low, Medium, High), title, description, and submit button. Keep the form short and operational.
6. Delay state: a focused utility sheet or screen where the driver can choose an estimated delay (5, 10, 15, 20+ minutes), enter an optional note, and confirm. Use clear warning styling.

Map integration guidance
- Show maps only in trip search/results and trip details, never as decorative backgrounds.
- Use a light, low-detail road map style with a thin UPTSLK-red route polyline, centre/start pin, destination pin, and optional live vehicle marker.
- Design map areas as replaceable Mapbox or Google Maps containers with a graceful static fallback for offline mode.

Deliverable requirements
- Produce all screens as an ordered, connected mobile flow with consistent components, typography, colors, form states, empty states, loading states, success states, and error states.
- Demonstrate navigation between Passenger screens and separately between Driver screens.
- Use the provided mock data consistently across every screen.
- Preserve a clean, realistic transport-operations experience. Avoid generic AI dashboard visuals and avoid adding features outside this scope.
```
