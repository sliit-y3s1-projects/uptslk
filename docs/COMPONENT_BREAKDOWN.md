## UPTS - Component Breakdown & Team Assignment Guide

UPTS is a platform that brings Sri Lanka's public bus network into one digital system. Commuters search, book, and track bus trips from their phone. Drivers see only the trips assigned to them and update status as they go. Admins manage the fleet, routes, and payments from a web dashboard. Behind the scenes, four AI agents work together to plan each booking, check real route and vehicle data, carry out the booking, and validate everything before it's finalized, pausing for human approval if something looks unusual. The whole system runs on one shared login, one database, and one consistent set of rules across web and mobile.

## Team Assignment

- **Component A:** [kishan-ahamed45](https://github.com/kishan-ahamed45)
- **Component B:** [RashmiK0119](https://github.com/RashmiK0119)
- **Component C:** [Nadeesha-D-Shalom](https://github.com/Nadeesha-D-Shalom)
- **Component D:** [chamals3n4](https://github.com/chamals3n4)

## Component A: Trip Booking & Ticketing

Lets commuters search for available bus trips and book a seat, generating a digital boarding pass. Handles the full lifecycle of a booking from creation to completion or cancellation.

### CRUD Operations

| Operation | Description                                                                                          |
| --------- | ---------------------------------------------------------------------------------------------------- |
| Create    | Create a new booking (select trip, seat, generate QR boarding pass)                                  |
| Read      | Search trips by origin, destination, time; view booking history; view single booking and its QR pass |
| Update    | Confirm, cancel, or reschedule a booking                                                             |
| Delete    | Cancel a booking (soft delete, kept for record, never fully removed)                                 |

**Also includes:** search/filter by status and date, sorting, pagination on booking history, and dynamic fare estimation based on distance, demand, and service type.

### Agent Part: Trip Planning Agent

This agent receives the commuter's raw request, for example "Kurunegala to Colombo, 8 AM." It doesn't touch the database or call any tool, its only job is to turn that request into a clear step-by-step plan: find matching routes, estimate fare, check seat availability, propose a booking. This plan is then passed to the next agent in the pipeline.

## Component B: Fleet & Route Management

Lets admins manage the buses, drivers, and routes that make up the network, and schedule trips by assigning a vehicle and driver to a route and time.

### CRUD Operations

| Operation | Description                                                                                                                |
| --------- | -------------------------------------------------------------------------------------------------------------------------- |
| Create    | Register a vehicle, register a driver, create a route with its ordered stops, schedule a trip                              |
| Read      | List vehicles, drivers, and routes; view a route's stop sequence; view trips (drivers see only their own assigned trips)   |
| Update    | Update vehicle/driver status, edit route details, reassign a trip's driver or vehicle, update trip status as it progresses |
| Delete    | Deactivate a vehicle or driver; delete a route only if no trips are linked to it                                           |

**Also includes:** filter by status/type, paginated fleet and trip lists, and route performance analytics (on-time percentage and average occupancy per route).

### Agent Part: Route & Demand Analysis Agent

This agent takes the plan from the previous agent and checks it against real data, which routes actually exist between the two points, how full those buses currently are, and which driver/vehicle are actually available at that time. It picks the best match and passes that decision forward.

## Component C: Wallet & Payment Management

Gives every commuter a digital wallet they can top up and pay fares from, and tracks every transaction so admins can reconcile driver payouts.

### CRUD Operations

| Operation | Description                                                                               |
| --------- | ----------------------------------------------------------------------------------------- |
| Create    | Top up a wallet, create a transaction (fare, refund, or payout)                           |
| Read      | View wallet balance, view transaction history, admin view of all transactions and payouts |
| Update    | Update transaction status, process a refund                                               |
| Delete    | No hard delete, financial records are never removed, only reversed with a new transaction |

**Also includes:** filter by transaction type and date range, paginated transaction history, and driver/operator payout reconciliation across completed trips.

### Agent Part: Booking & Dispatch Agent

This is the only agent that actually performs real actions. It takes the route/vehicle decision from the previous agent and calls a fixed set of tools: a maps API to confirm distance and time, a fare calculator, a booking-creation tool that writes the booking into the database, and a notification tool that confirms the trip to the commuter.

## Component D: Incident & Safety Reporting + Agent Oversight

Lets commuters and drivers report incidents like delays or safety issues, tracks how quickly they're resolved, and owns the shared system that ties all four AI agents' work together, including the point where a human has to approve anything unusual.

### CRUD Operations

| Operation | Description                                                                                             |
| --------- | ------------------------------------------------------------------------------------------------------- |
| Create    | Report an incident linked to a trip; create an approval request when the system flags something unusual |
| Read      | List incidents, view incident detail, view AI workflow history for a booking, view pending approvals    |
| Update    | Update incident status as it's resolved; approve, reject, or send back a flagged AI action              |
| Delete    | No hard delete, incidents and approval records are kept as a permanent audit trail                      |

**Also includes:** filter by incident type/status/trip, paginated incident list, and incident trend analytics (recurring issues by driver, vehicle, or route).

### Agent Part: Compliance & Validation Agent

This agent runs last. It checks the outcome of the booking against fixed rules, is the fare in a normal range, is the vehicle within capacity, and looks for anything unusual, like a sudden fare spike or a last-minute driver change. If everything checks out, the booking is finalized. If not, it pauses the workflow and sends it to an admin for approval before anything is confirmed.

## Technology Stack by Component

Each dot shows which application layer that component is built on.

🔴 = Backend API (ASP.NET Core) &nbsp;&nbsp; 🔵 = React Web App &nbsp;&nbsp; 🟢 = Flutter Mobile App

| Component                               | Backend (ASP.NET Core) | React Web |    Flutter Mobile     |
| --------------------------------------- | :--------------------: | :-------: | :-------------------: |
| A - Trip Booking & Ticketing            |           🔴           |    🔵     |          🟢           |
| B - Fleet & Route Management            |           🔴           |    🔵     | 🟢 (driver view only) |
| C - Wallet & Payment                    |           🔴           |    🔵     |          🟢           |
| D - Incident & Safety + Agent Oversight |           🔴           |    🔵     |   🟢 (report only)    |

**How the two frontends differ:**

- **React (web)** is used only by Admins, managing fleet, routes, payments, incidents, and reviewing AI agent decisions.
- **Flutter (mobile)** is used by Commuters (search, book, pay, track, report) and Drivers (view assigned trips, update status, report incidents). Drivers never see fleet/route management screens, they only see what's assigned to them.

## Shared Foundation (used by all 4 components)

| Layer               | Technology                                                                                     |
| ------------------- | ---------------------------------------------------------------------------------------------- |
| Backend Framework   | ASP.NET Core Web API (C#)                                                                      |
| Database            | PostgreSQL, via Entity Framework Core                                                          |
| Authentication      | ASP.NET Core Identity with JWT-based login, shared across React and Flutter                    |
| Web Frontend        | React                                                                                          |
| Mobile Frontend     | Flutter                                                                                        |
| Agent Orchestration | Shared workflow state (AgentWorkflow, AgentStep, ApprovalRequest tables), owned by Component D |

## Rough Idea: How to Implement the Agent Workflow / Orchestration

This is just a simple starting idea, not a fixed design, refine it once you actually start building it.

**Basic concept:** the 4 agents are not 4 separate always-on services. Think of them as 4 functions that run one after another, in order, every time a booking request comes in. One function's output becomes the next function's input.

**Where it lives:** all 4 agent functions live inside the ASP.NET Core backend, as a single service, something like `AgentOrchestrationService`. No separate app or server needed for this. Keeps things simple for a student project, one codebase, one deployment.

**How a run actually happens, step by step:**

1. Commuter submits a booking request.
2. Backend creates one row in `AgentWorkflow` (status = Running).
3. Backend calls `Agent1_Plan()` → saves its output as a row in `AgentStep`.
4. Backend calls `Agent2_Analyze()`, passing Agent 1's output → saves another `AgentStep` row.
5. Backend calls `Agent3_BookAndDispatch()`, passing Agent 2's output → this one actually creates the real Booking record, calls the maps API, calls the fare calculator → saves its result as a `AgentStep` row.
6. Backend calls `Agent4_Validate()` → checks fixed rules (fare range, capacity). If OK, mark `AgentWorkflow` as Completed. If something looks off, mark it as PausedForApproval and create an `ApprovalRequest` row.
7. Admin sees pending approvals in React, approves or rejects, workflow updates to Completed or Failed.

**Do the agents need to actually be "AI" (LLM calls)?**
Not necessarily for all 4. A simple, honest way to think about it:

- Agent 1 (Planning) and Agent 2 (Analysis) are good candidates for an actual LLM call (e.g. OpenAI/Claude API), since they involve reasoning over a flexible request.
- Agent 3 (Booking/Dispatch) can be plain C# code that calls your existing APIs/tools in sequence, no LLM needed, it's just executing a plan.
- Agent 4 (Validation) can also be plain C# rule-checking (if fare > X, if capacity exceeded), not necessarily an LLM either.

Using an LLM only where real reasoning is needed, and plain code where it's just fixed rules or tool calls, is a completely valid and honestly a stronger design than forcing all 4 to be LLM calls.

**Suggested simple tech pieces:**

- One LLM API (OpenAI, Claude, or even a free-tier model) called from the backend for Agent 1 and Agent 2 only.
- Plain C# service methods for Agent 3 and Agent 4.
- `AgentWorkflow`, `AgentStep`, `ApprovalRequest` tables (already in the schema) to store everything, so React can display the full run afterwards.
- No message queue, no separate microservice, no LangChain/LangGraph required for a project this size, a single service class calling 4 methods in order is enough to satisfy the assignment's requirements.
