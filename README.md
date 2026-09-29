# UPTSLK

### Unified Public Transport System

UPTSLK is a full-stack public transport operations and seat-reservation platform developed for the SLIIT Software Engineering Framework module. It connects passenger booking, centre operations, fleet management, driver workflows, incident handling, and supervised AI-assisted recovery in one system.

> **Academic prototype:** UPTSLK is a fictional university project created for education and demonstration. It is not an official Sri Lankan transport service, is not affiliated with a transport authority, and must not be used as a production booking, payment, dispatch, or safety system.

[Quick start](#quick-start) · [Technology](#technology) · [Documentation](#documentation) · [Contributing](CONTRIBUTING.md) · [GitHub](https://github.com/sliit-y3s1-projects/uptslk)

## What UPTSLK covers

### Passengers

- Search scheduled departures by origin, destination, and date.
- Reserve seats and complete test payments through Stripe Checkout.
- Access bookings, digital tickets, and QR boarding passes.
- Receive in-app updates when an approved recovery affects a trip.
- Manage commuter profile information and profile images.

### Centre operations

- Manage routes, directions, timetables, bays, trips, vehicles, and drivers.
- Track duty assignments, maintenance, passenger flow, and incidents.
- Start supervised recovery assessments for disrupted trips.
- Review deterministic safety evidence before approving operational changes.

### Administration and drivers

- Manage organizations, centres, operational users, and access roles.
- Provide mobile authentication and trip workflows for registered drivers.
- Keep centre-level data and actions restricted to authorized users.

### Agent-assisted recovery

The incident recovery subsystem uses Google Gemini as a bounded planner and four specialized deterministic agents for network continuity, fleet readiness, dispatch recovery, and passenger impact. Operational rules remain in ASP.NET Core, and vehicle, driver, bay, schedule, and passenger changes require deterministic validation and authorized human approval.

The model never receives unrestricted database access and cannot approve or directly apply a recovery action.

The API is the system boundary for authentication, authorization, business rules, persistence, payment verification, storage access, AI orchestration, and audit records. Web and mobile clients do not receive provider secrets or direct database access.

## Technology

| Area | Technology | Use |
| --- | --- | --- |
| Web | React, TypeScript, Vite | Passenger, centre operations, and administrator interfaces |
| Web data | TanStack Query | API state, caching, and request lifecycle management |
| UI system | Base UI, shadcn/ui, Tailwind CSS | Accessible primitives, shared components, and consistent styling |
| Mobile | Flutter and Dart | Passenger booking, tickets, profiles, and driver workflows |
| API | ASP.NET Core 8 | REST endpoints, authorization, orchestration, and business rules |
| Data access | Entity Framework Core and Npgsql | PostgreSQL persistence and committed schema migrations |
| Database | PostgreSQL 17 | Operational, identity, booking, payment, and audit data |
| Authentication | ASP.NET Core Identity and JWT | Account management and role-based access |
| Payments | Stripe Checkout and webhooks | Test booking payments, reconciliation, and refunds |
| File storage | Supabase Storage | Profile and vehicle image uploads |
| Agentic AI | Google Gemini through `Google.GenAI` | Structured incident-recovery planning and bounded replanning |
| API documentation | OpenAPI and Swagger | Local endpoint discovery and testing |
| Testing | xUnit, ASP.NET Core test host, SQLite | Unit, database-backed, and authenticated HTTP integration tests |
| Local infrastructure | Docker Compose and Make | PostgreSQL and repeatable development commands |

Stripe, Supabase, and Gemini are external providers. Their secret keys belong in local or deployment configuration and must never be committed to the repository.

## Repository structure

```text
apps/
  api/             ASP.NET Core API, domain models, services, and migrations
  api.Tests/       Unit and integration tests
  api.Evaluation/  Agent recovery dataset and evaluation runner
  web/             React and TypeScript web platform
  mobile/          Flutter passenger and driver application
docs/               Architecture, module, provider, and evaluation documentation
docker-compose.yml  Local PostgreSQL infrastructure
Makefile            Common setup, run, migration, and validation commands
```

## Quick start

Prerequisites are Docker, the .NET 8 SDK with EF Core CLI, Node.js, pnpm, and Git.

```bash
make setup
make dev
```

The web app normally runs at `http://localhost:5173`, and the API runs at `http://localhost:5250`.

For environment configuration, manual setup, migrations, validation commands, and pull-request rules, follow the [contribution guide](CONTRIBUTING.md).

## Documentation

- [Contribution and complete development guide](CONTRIBUTING.md)
- [Agent subsystem architecture and progress](docs/AGENT_SUBSYSTEM.md)
- [Agent recovery evaluation guide](docs/evaluation/README.md)
- [Stripe test-payment setup](docs/STRIPE_SETUP_GUIDE.md)
- [Software requirements specification](docs/Unified%20Public%20Transport%20System%20%28UPTS%29%20-%20SRS.pdf)

## Contributing

Keep changes focused, preserve centre-level access boundaries, never commit real provider credentials, and run the relevant API, web, or mobile checks before opening a pull request.

Start with [CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow and repository conventions.
