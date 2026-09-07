# UPTS - Component Breakdown and Team Assignment

UPTSLK is a centre-based public-transport platform. Commuters search, book, pay for, and track bus trips in Flutter. Drivers use Flutter to see their assignments, update a trip, and report incidents. Centre Managers operate their own multimodal centre in React; Super Admins govern centres, employees, roles, and platform health.

The four components below are the team's distinct major CRUD features. The four-agent service-recovery workflow is a shared assessed feature that uses data from each component; it does not replace their CRUD work.

## Team Assignment

| Component | Member | Major functionality | Primary entity |
| --- | --- | --- | --- |
| A | [kishan-ahamed45](https://github.com/kishan-ahamed45) | Centres and network | Centre / Route |
| B | [RashmiK0119](https://github.com/RashmiK0119) | Fleet and maintenance | Vehicle |
| C | [chamals3n4](https://github.com/chamals3n4) | Scheduling and dispatch | Trip |
| D | [Nadeesha-D-Shalom](https://github.com/Nadeesha-D-Shalom) | Passengers and fares | Booking |

## A: Centres and Network

Owns multimodal centres, routes, ordered stops, bays, timetables, and centre-scoped network information.

| Operation | Scope |
| --- | --- |
| Create | Centre, route, stop sequence, bay, and timetable |
| Read | Search/list centres and routes; view centre profiles, route details, bays, and timetables |
| Update | Centre details, route stops, bay availability, timetable, and operating status |
| Delete | Deactivate a centre/bay or archive an unused route |

Includes district/status filters, route search, pagination, and network-health summaries.

**Agent: Network Continuity Agent.** Reads centre, route, bay, and timetable data to propose an alternative bay or time slot when a trip is disrupted. It cannot change operational data.

## B: Fleet and Maintenance

Owns vehicles and their maintenance/inspection readiness within a centre.

| Operation | Scope |
| --- | --- |
| Create | Vehicle and maintenance/inspection record |
| Read | Search/list vehicles; view profile, documents, maintenance history, and assignments |
| Update | Vehicle status, capacity, accessibility features, maintenance, and readiness |
| Delete | Deactivate a vehicle while retaining operational history |

Includes centre/type/readiness filters, vehicle images, pagination, and maintenance-due alerts.

**Agent: Fleet Readiness Agent.** Ranks replacement vehicles using availability, capacity, accessibility requirements, inspection status, and maintenance blocks. It returns a structured result and cannot assign a vehicle.

## C: Scheduling and Dispatch

Owns planned and live trips: route, vehicle, driver, bay, timetable, dispatch state, and incident handling.

| Operation | Scope |
| --- | --- |
| Create | Trip with route, departure, vehicle, driver, and bay |
| Read | Filtered dispatch boards; trip detail, assignments, history, and incidents |
| Update | Reassign trip resources, progress lifecycle, delay/cancel a trip, resolve an incident |
| Delete | Cancel/archive a trip while retaining its record |

Includes recurring services, bay boards, conflict detection, approval queues, and dispatch history. Drivers see only their own assignments in Flutter.

**Agent: Dispatch Recovery Agent.** Combines eligible vehicle options, driver availability, and an available bay/time slot into a ranked recovery proposal. It reads only trip, driver, and schedule data; it cannot change a trip.

## D: Passengers and Fares

Owns commuter bookings, seats, boarding passes, wallet/payment records, refunds, and passenger service updates.

| Operation | Scope |
| --- | --- |
| Create | Booking, seat allocation, fare payment, wallet top-up, refund, or transfer proposal |
| Read | Trip search, booking/QR pass, wallet balance, transaction history, and service updates |
| Update | Confirm, cancel, or reschedule booking; apply an approved refund or transfer |
| Delete | Soft-cancel a booking; reverse rather than delete financial records |

Includes fare estimation, seat maps, booking/payment history, pagination, and commuter incident reporting.

**Agent: Passenger and Fare Impact Agent.** Determines affected passengers, capacity/accessibility impact, notifications, and a transfer/refund/credit proposal. It cannot modify bookings or issue money.

## Shared Foundation

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core Web API and C# |
| Database | PostgreSQL through Entity Framework Core |
| Authentication | ASP.NET Core Identity and JWT |
| Admin application | React |
| Mobile application | Flutter |
| Agent workflow | Shared workflow state, validation, approval, and audit trail |

React is for Super Admin and Centre Manager work. Flutter is for commuters and drivers. See [Agentic AI Service-Recovery Workflow](AGENTIC_AI_WORKFLOW.md) for the shared assessed workflow.
