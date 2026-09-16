# UPTSLK Project Progress

Last reviewed: 16 September 2026

This document records the current implementation state after the four CRUD components were merged. `✅ DONE` means the code exists in the repository. It does not necessarily mean that the complete workflow has been manually demonstrated against a running database.

## Foundation

- ✅ DONE — ASP.NET Core Web API and PostgreSQL/EF Core setup.
- ✅ DONE — Database models, DTOs, controllers, and committed migrations for the four core components.
- ✅ DONE — React/Vite administrator shell, centre-scoped layouts, reusable UI components, API client, and TanStack Query provider.
- ✅ DONE — Root `Makefile`, `CONTRIBUTING.md`, component allocation, milestone, mobile, and Agentic AI documentation.
- ✅ DONE — Local Identity/JWT authentication now carries role and optional centre assignment; startup only ensures the configured bootstrap admin exists and the demo login panel is removed.
- ⏳ NOT DONE — Apply the pending user-centre schema migration, provision the first admin through registration, and complete centre-level access enforcement.

## Component A — Centres and Network

Owner: Kishan Ahamed

- ✅ DONE — Centre API: list, search/filter, detail, create, update, and close.
- ✅ DONE — Bay API: list, detail, create, update, and deactivate.
- ✅ DONE — Route API: list/search, detail, create, update, and archive.
- ✅ DONE — Ordered route stops and route schedule API contracts.
- ✅ DONE — React API services and TanStack hooks for centres, bays, routes, and schedules.
- ✅ DONE — API smoke check against the running server: centres, routes, and related schedule endpoints respond successfully.
- ✅ DONE — Super Admin centre list/create/edit/profile screens now use the centre API hooks; employee/manager fields remain explicit placeholders until the employee API is available.
- ⏳ NOT DONE — Replace the centre operations console’s empty/mock departure presentation with live trip/schedule data.
- ⏳ NOT DONE — Full manual CRUD verification in the browser, including persistence after refresh and conflict/error states.

## Component B — Fleet and Maintenance

Owner: Rashmi

- ✅ DONE — Vehicle, driver, and maintenance screens with centre filtering and detail/form routes.
- ✅ DONE — API services, hooks, types, and CRUD wiring for fleet and maintenance records.
- ✅ DONE — API smoke check against the running server: vehicles, drivers, and maintenance endpoints respond successfully.
- ⏳ NOT DONE — Complete runtime CRUD walkthrough against PostgreSQL and final UI/API edge-case verification.

## Component C — Scheduling and Dispatch

Owner: Chamal

- ✅ DONE — Trip, dispatch, lifecycle, incident, bay, approval, and history UI screens.
- ✅ DONE — Backend trip, incident, and scheduling API foundations.
- ✅ DONE — React API services and TanStack hooks for trip listing, history, detail, creation, editing, status transitions, reassignment, cancellation, and incidents.
- ✅ DONE — Dispatch board, trip forms, trip detail, trip history, and incident reporting now use persisted API data.
- ✅ DONE — API smoke check against the running server: trips and incidents endpoints respond successfully.
- ✅ DONE — Bay-operations view now reads centre bays and active trip assignments from the API and persists bay availability changes.
- ⏳ NOT DONE — Connect the approval queue to the future Agentic AI approval/workflow API; no backend approval endpoint exists yet.
- ⏳ NOT DONE — Complete browser verification of trip reassignment, status transitions, cancellation, and incident resolution.
- ⏳ NOT DONE — Flutter driver trip-assignment and incident-report integration.

## Component D — Passengers and Fares

Owner: Nadeesha

- ✅ DONE — Passenger profiles, bookings, tickets, seat selection, manifests, fare rules, wallet/payment views, and reconciliation screens.
- ✅ DONE — Backend APIs and React services/hooks for passengers, bookings, fare rules, and related flows.
- ✅ DONE — API smoke check against the running server: passengers, fare rules, and bookings endpoints respond successfully.
- ⏳ NOT DONE — End-to-end booking-to-payment/refund testing with real persisted data.

## Mobile Application

- ✅ DONE — Mobile scope, users, screens, and driver/commuter responsibilities documented in `docs/MOBILE.md`.
- ⏳ NOT DONE — Flutter application implementation and API integration.

## Agentic AI and Final Hardening

- ✅ DONE — Four-agent service-recovery design, contracts, tools, approval flow, validation rules, and audit requirements documented.
- ⏳ NOT DONE — Durable workflow/approval tables and orchestration implementation.
- ⏳ NOT DONE — Four distinct agents, allow-listed tools, deterministic validation, approval pause, safe failure, and audit UI.
- ⏳ NOT DONE — Automated tests, security review, pagination/error standardization, final screenshots, and demonstration evidence.

## Current Verification

- ✅ DONE — `pnpm lint` completes with warnings only.
- ✅ DONE — `pnpm build` completes successfully.
- ✅ DONE — Local PostgreSQL container is running and EF reports the database is up to date.
- ✅ DONE — HTTP smoke checks returned `200` for `/centres`, `/routes`, `/vehicles`, `/drivers`, `/maintenance-records`, `/trips`, `/incidents`, `/passengers`, `/fare-rules`, and `/bookings`.
- ⏳ NOT DONE — Full API-backed browser test of every component and the assessed Agentic AI workflow.
