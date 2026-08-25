# UPTS - Development Milestones (Backend First)

## Important Notes Before Starting

- The current database structure is a **starting point, not final**. As you build each part, you may find a field, relationship, or table needs to change. That's expected, discuss any schema change with the team before applying it, since it affects everyone's migrations.
- **Agentic AI is intentionally left out of this milestone list for now.** Focus is only on getting the core backend APIs working and stable first. Agent work will be a separate milestone list once this is done.
- For now, the whole team should focus on **backend only** (ASP.NET Core + PostgreSQL). React and Flutter work comes after each backend piece is stable and testable via Swagger/Postman.
- Mark each row's status as you go: `Not Started`, `In Progress`, or `Done`.
- With only 4 people, work through this **in order, top to bottom**. Later milestones depend on earlier ones, so it's fine, and expected, for someone to wait until the milestone before theirs is done rather than starting early on empty data.

---

## Milestone 1: Vehicles & Drivers (Component B)

| #   | Task                                                                  |   Status    |
| --- | --------------------------------------------------------------------- | :---------: |
| 1.1 | Add, list, update, and deactivate vehicles, with search and filtering | Not Started |
| 1.2 | Create driver accounts (admin only) and link them to the driver table | Not Started |
| 1.3 | List and update drivers, with filtering by status                     | Not Started |
| 1.4 | Test everything using the seeded admin account                        | Not Started |

---

## Milestone 2: Routes (Component B)

| #   | Task                                                                             |   Status    |
| --- | -------------------------------------------------------------------------------- | :---------: |
| 2.1 | Create a route along with its ordered list of stops                              | Not Started |
| 2.2 | List and search routes by origin/destination                                     | Not Started |
| 2.3 | View, update, and delete routes (only delete if unused)                          | Not Started |
| 2.4 | Seed a few realistic routes, including same-destination, different-path examples | Not Started |

---

## Milestone 3: Trip Scheduling (Component B)

| #   | Task                                                                         |   Status    |
| --- | ---------------------------------------------------------------------------- | :---------: |
| 3.1 | Schedule a trip by assigning a route, vehicle, driver, and time              | Not Started |
| 3.2 | List all trips for admins, with filters by status/date/route                 | Not Started |
| 3.3 | Let a driver see only the trips assigned to them                             | Not Started |
| 3.4 | Update trip status as it progresses, and reassign driver/vehicle when needed | Not Started |
| 3.5 | Add route performance summary (on-time rate, average occupancy)              | Not Started |

_Component B is fully done after this milestone._

---

## Milestone 4: Trip Booking & Ticketing (Component A)

Can only start once Milestone 3 is done, since bookings need real trips to book against.

| #   | Task                                                                       |   Status    |
| --- | -------------------------------------------------------------------------- | :---------: |
| 4.1 | Search available trips by origin, destination, and time                    | Not Started |
| 4.2 | Create a booking with seat selection and a generated boarding pass         | Not Started |
| 4.3 | List and view a commuter's own bookings, with pagination and status filter | Not Started |
| 4.4 | Cancel or reschedule a booking within policy rules                         | Not Started |
| 4.5 | Add fare estimation based on distance, demand, and service type            | Not Started |
| 4.6 | Test the full search-to-booking flow with a seeded commuter account        | Not Started |

_Component A is fully done after this milestone._

---

## Milestone 5: Wallet & Payment (Component C)

Can only start once Milestone 4 is done, since fare deduction needs real bookings to attach to.

| #   | Task                                                                     |   Status    |
| --- | ------------------------------------------------------------------------ | :---------: |
| 5.1 | Auto-create a wallet when a commuter registers                           | Not Started |
| 5.2 | Top up wallet and view balance                                           | Not Started |
| 5.3 | List and filter transaction history, with pagination                     | Not Started |
| 5.4 | Deduct fare automatically when a booking is confirmed                    | Not Started |
| 5.5 | Support refunds against a booking                                        | Not Started |
| 5.6 | Build a payout reconciliation summary for drivers/vehicles over a period | Not Started |
| 5.7 | Test the full top-up to fare-deduction flow end to end                   | Not Started |

_Component C is fully done after this milestone._

---

## Milestone 6: Incident & Safety Reporting (Component D)

Can start as soon as Milestone 3 is done (only needs trips to exist), doesn't need to wait for Milestones 4 or 5.

| #   | Task                                                                     |   Status    |
| --- | ------------------------------------------------------------------------ | :---------: |
| 6.1 | Let commuters and drivers report an incident linked to a trip            | Not Started |
| 6.2 | List and filter incidents by type, status, and trip, with pagination     | Not Started |
| 6.3 | View a single incident's full detail                                     | Not Started |
| 6.4 | Update incident status as it gets resolved, with SLA due date tracking   | Not Started |
| 6.5 | Add an incident trend summary (recurring issues by driver/vehicle/route) | Not Started |
| 6.6 | Test incident reporting from both a commuter and a driver seeded account | Not Started |

_Component D is fully done after this milestone._

---

## Milestone 7: Reports & Dashboard (Cross-Component)

Needs all four components done, since it pulls data from all of them.

| #   | Task                                                                     |   Status    |
| --- | ------------------------------------------------------------------------ | :---------: |
| 7.1 | General dashboard summary (active trips, bookings today, open incidents) | Not Started |
| 7.2 | Revenue and booking trends over time                                     | Not Started |

---

## Milestone 8: Backend Hardening (Cross-Component)

Final pass across the whole API, everyone reviews their own component's endpoints.

| #   | Task                                                                         |   Status    |
| --- | ---------------------------------------------------------------------------- | :---------: |
| 8.1 | Review role-based access on every endpoint                                   | Not Started |
| 8.2 | Add proper input validation everywhere data is created or updated            | Not Started |
| 8.3 | Make sure API responses follow one consistent format                         | Not Started |
| 8.4 | Double check pagination, filtering, and sorting work the same way everywhere | Not Started |
| 8.5 | Add basic, consistent error handling across the whole API                    | Not Started |
