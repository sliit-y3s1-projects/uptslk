# Agentic AI: Service-Recovery Workflow

## Purpose

UPTSLK uses a supervised multi-agent workflow when an active service has a breakdown, severe delay, or safety issue. A driver reports the event in Flutter, a Centre Manager reviews the proposed action in React, and ASP.NET Core controls all data access and changes. This is not a general chatbot: every agent has a defined responsibility, input/output contract, restricted tools, and audit trail.

## Assessed Scenario

For example, a driver reports a breakdown on the 11:00 Kadawatha–Kaduwela service. The system finds a workable bay/time slot, a roadworthy replacement bus, an eligible driver, and the passenger/fare effect. It validates the proposal, pauses for authorized approval, then either applies the approved recovery or records a safe failure.

```text
Flutter driver report -> four agent proposals -> rule validation
  -> Centre Manager approval in React -> approved recovery / safe failure
```

## Agents and Ownership

| Component | Agent | Input | Structured output | Allowed data/tools |
| --- | --- | --- | --- | --- |
| A: Centres & Network | Network Continuity Agent | Incident, centre, route, departure | Alternative bay/time slot and route constraints | Read centres, routes, bays, timetables |
| B: Fleet & Maintenance | Fleet Readiness Agent | Incident and replacement requirements | Ranked roadworthy vehicles with reasons | Read fleet, capacity, inspections, maintenance |
| C: Scheduling & Dispatch | Dispatch Recovery Agent | Incident plus network/fleet options | Ranked vehicle, driver, bay, and revised-departure proposal | Read trips, driver availability, schedules |
| D: Passengers & Fares | Passenger & Fare Impact Agent | Affected bookings and recovery proposal | Passenger impact, capacity result, notifications, transfer/refund/credit proposal | Read bookings, seats, fares, payments; create proposal only |

Each agent uses validated DTOs and returns schema-validated JSON. No agent can directly change a trip, booking, payment, vehicle, or bay.

## Orchestration and Approval

1. A validated driver incident report creates an `AgentWorkflow` with `Running` status.
2. The orchestrator saves the domain objective and a structured step plan, then invokes all four agents.
3. Each input, output, timing, retry, error, validation result, and tool call is saved as an `AgentStep` or tool-call record.
4. A shared `RecoveryValidationService` checks vehicle readiness, driver duty/eligibility, bay conflict, seat capacity, fare policy, trip-state transition, and centre scope.
5. The workflow creates an `ApprovalRequest` and moves to `PausedForApproval` before a reassignment, cancellation, or financial action.
6. The owning Centre Manager or a Super Admin may approve, reject, or request revision. Rejection records a safe failure.
7. `ApprovedRecoveryExecutor` re-checks the approval, role, centre scope, and validation results before applying permitted updates and notifying affected users.

## Allow-Listed Tools and Safety

Allowed backend tools include `GetTripSnapshot`, `GetAffectedBookings`, `GetRouteSchedule`, `FindAvailableBays`, `FindAvailableVehicles`, `FindAvailableDrivers`, `GetFareAndPayment`, `ValidateVehicleReadiness`, `ValidateDriverEligibility`, `ValidateBayConflict`, `CreateApprovalRequest`, and `SendServiceNotification`. Write operations are isolated to `ApplyApprovedRecovery`, which requires an approved workflow.

PostgreSQL retains workflow ID, requester, centre, trip, incident, objective, plan, steps, tool inputs/outputs, validation results, approval decision, final outcome, errors, and retries. Invalid, unsupported, timed-out, or unsafe requests fail safely and visibly. An LLM may help interpret a free-text report or draft a plan, but it has no unrestricted database or tool access.
