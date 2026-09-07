# UPTS - Development Milestones

## Working Rules

- The schema is a starting point. Discuss cross-component database changes before creating migrations.
- Each component must provide full CRUD, validation, authorization, filtering/pagination where relevant, and a functional API before its React/Flutter screens are integrated.
- Agentic AI follows the core CRUD work because it relies on real centre, fleet, trip, booking, and fare data.
- Mark each task `Not Started`, `In Progress`, or `Done`.

## Milestone 1: Centres and Network (Component A)

| # | Task | Status |
| --- | --- | :---: |
| 1.1 | Create, list, update, and deactivate multimodal centres with district/status filtering | Not Started |
| 1.2 | Create routes with ordered stops, centre ownership, and timetable information | Not Started |
| 1.3 | Manage centre bays and bay availability | Not Started |
| 1.4 | Add centre profiles, route search, and centre-scoped authorization | Not Started |

## Milestone 2: Fleet and Maintenance (Component B)

| # | Task | Status |
| --- | --- | :---: |
| 2.1 | Add, list, update, and deactivate vehicles with centre/type/readiness filtering | Not Started |
| 2.2 | Create and view inspection and maintenance records | Not Started |
| 2.3 | Update vehicle readiness, capacity, and accessibility information | Not Started |
| 2.4 | Test fleet and maintenance actions with seeded accounts | Not Started |

## Milestone 3: Scheduling and Dispatch (Component C)

| # | Task | Status |
| --- | --- | :---: |
| 3.1 | Create trips with route, vehicle, driver, bay, and departure time | Not Started |
| 3.2 | Build centre/date/status-filtered dispatch boards and trip detail | Not Started |
| 3.3 | Let drivers view/update only their assigned trips in Flutter | Not Started |
| 3.4 | Reassign resources, progress trip status, delay/cancel trips, and retain history | Not Started |
| 3.5 | Add recurring timetable services, conflict detection, and basic route performance | Not Started |

## Milestone 4: Passengers and Fares (Component D)

Requires scheduled trips.

| # | Task | Status |
| --- | --- | :---: |
| 4.1 | Search trips and create bookings with seat selection and QR boarding pass | Not Started |
| 4.2 | View, filter, cancel, and reschedule a commuter's bookings | Not Started |
| 4.3 | Estimate fares and manage wallet top-ups, balance, and transaction history | Not Started |
| 4.4 | Deduct fare on confirmation; process approved refunds or credits | Not Started |
| 4.5 | Test the complete commuter search-to-payment flow | Not Started |

## Milestone 5: Agentic Service Recovery (Cross-Component)

Requires the previous milestones and the driver incident-report flow.

| # | Task | Status |
| --- | --- | :---: |
| 5.1 | Persist workflow, step, tool-call, validation, and approval audit data | Not Started |
| 5.2 | Implement the four member-owned agents with DTO contracts and allow-listed tools | Not Started |
| 5.3 | Add deterministic rules for readiness, duty eligibility, bay conflict, capacity, fare policy, and centre scope | Not Started |
| 5.4 | Pause reassignment, cancellation, and financial action for authorized manager approval | Not Started |
| 5.5 | Implement approved execution plus rejection, revision, timeout, and safe-failure paths | Not Started |
| 5.6 | Demonstrate driver report -> agent workflow -> manager approval -> service update across Flutter, React, and API | Not Started |

See [Agentic AI Service-Recovery Workflow](AGENTIC_AI_WORKFLOW.md) for the assessed scenario.

## Milestone 6: Reports and Dashboards (Cross-Component)

| # | Task | Status |
| --- | --- | :---: |
| 6.1 | Centre dashboard summary: active trips, fleet readiness, bookings, and incidents | Not Started |
| 6.2 | Reports for route performance, ridership, revenue, maintenance, and recovery outcomes | Not Started |

## Milestone 7: Hardening and Demonstration (Cross-Component)

| # | Task | Status |
| --- | --- | :---: |
| 7.1 | Review role-based access and centre data isolation on all endpoints | Not Started |
| 7.2 | Apply consistent DTO validation, pagination, error responses, and audit logging | Not Started |
| 7.3 | Run backend, React, Flutter, CRUD, and agent-workflow demonstration tests | Not Started |
| 7.4 | Prepare ADR, test evidence, screenshots, and each member's explanation/demo | Not Started |
