# Support and assistance request workflow

**Audience:** Component D maintainers and staff-workflow testers.

**Baseline:** `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16. This describes the existing persisted API, independently of whether a particular UI uses it.

## API access and operations

[SupportRequestsController](../../apps/backend/api/Controllers/SupportRequestsController.cs) restricts all routes under `/api/v1/support-requests` to Admin, CentreManager, or Dispatcher.

| Method / route | Purpose | Success response |
| --- | --- | --- |
| `GET /support-requests` | List and filter cases | 200 with case responses. |
| `GET /support-requests/{id}` | Read a case | 200, or 404 for an unknown ID. |
| `POST /support-requests` | Create a case | 201 with ID/status and a location for the read action. |
| `PUT /support-requests/{id}` | Update case handling details | 204. |

The service does not expose deletion, a commuter self-service create route, or a comment-thread endpoint here. AssignedTo is optional text, not a validated foreign key to a staff account. Do not infer assignment permissions or a notification from entering a person's name.

## Creation and linked context

Creation accepts optional PassengerId, TripId, and CentreId; Type is Assistance or Support, and Priority is Low, Medium, High, or Urgent. Defaults are Support and Medium. New records start Open.

Subject and Description are required, limited to 200 and 2,000 characters. AssignedTo is limited to 160. The service trims subject/description and cleans optional assignment text. It checks that supplied passenger and trip IDs exist. It does not establish that the passenger booked that trip, and it does not explicitly check centre ownership or validate CentreId in this service. Database relationships remain separate from business authorization.

Sources: [request DTOs](../../apps/backend/api/DTO/Support/SupportRequests.cs), [SupportRequestService](../../apps/backend/api/Services/SupportRequestService.cs), and [SupportRequest entity](../../apps/backend/api/Entities/SupportRequest.cs).

## Updating status and resolution

Available statuses are Open, InProgress, WaitingForPassenger, Resolved, and Closed. Update replaces priority, status, subject, description, assignment, and resolution. It does not replace passenger/trip/centre links or case type. Resolution text is optional and limited to 2,000 characters.

ResolvedAt is set to the current UTC time when an update uses Resolved or Closed; updates to another status clear it. Updating an already-resolved case resets that timestamp to the update time, so it is not an immutable first-resolution audit field. UpdatedAt changes on each update.

The service does not enforce an ordered transition graph, require a nonempty resolution for closure, or define response-time targets. A team's desired support process should not be presented as an implemented validation rule. Source: [status enums](../../apps/backend/api/Entities/Enums/SupportRequestEnums.cs) and the service above.

## Search and operational boundaries

The [repository](../../apps/backend/api/Repositories/SupportRequestRepository.cs) supports Type, Status, PassengerId, and TripId filters and orders results newest first. [SupportRequestFilter](../../apps/backend/api/DTO/Support/SupportRequestFilter.cs) has no CentreId filter. Current list queries do not add a caller-centre restriction; controller role access must not be described as proof of centre isolation.

Opening or closing a support record does not itself cancel a booking, change a wallet, request a refund, or dispatch a vehicle. Carry out those operations through their own authorized workflows and retain the related references in controlled evidence.

## Review checks

Verify trimmed creation, nonexistent linked IDs, required-field errors, list filters, a resolved update, reopening that clears ResolvedAt, unknown IDs, and denial for an anonymous/Commuter caller. Add explicit review coverage if centre isolation or enforced status progression is proposed later; neither is claimed here.

Existing checks are in [SupportAndNotificationTests](../../apps/backend/api.Tests/Bookings/SupportAndNotificationTests.cs) and [FareAndPaymentAccessControlTests](../../apps/backend/api.Tests/Bookings/FareAndPaymentAccessControlTests.cs). Use synthetic descriptions in shared evidence and keep passenger contact details out of public issue bodies.
