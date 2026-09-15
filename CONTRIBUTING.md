# Contributing to UPTSLK

UPTSLK is a centre-based public-transport platform with an ASP.NET Core API, PostgreSQL database, React admin console, and Flutter mobile application. Keep changes focused on the feature you own.

## Quick start (Makefile)

### Table of contents

- [Prerequisites](#prerequisites)
- [One-command setup](#one-command-setup)
- [Run the applications](#run-the-applications)
- [Useful Make targets](#useful-make-targets)

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK and EF Core CLI
- Node.js and pnpm
- Git

### One-command setup

From the repository root:

```bash
make setup
```

This installs dependencies, creates `apps/web/.env` from `.env.example`, starts PostgreSQL, and applies committed migrations.

### Run the applications

```bash
make dev
```

This starts PostgreSQL, applies migrations, runs the API, and runs the Vite web app. The API is available at `http://localhost:5250`; the web app is normally at `http://localhost:5173`.

### Useful Make targets

```bash
make db-up             # Start PostgreSQL
make db-down           # Stop PostgreSQL
make db-logs           # Follow database logs
make db-migrate        # Apply committed migrations
make api               # Run only the API
make web               # Run only the web app
make check             # API build, frontend lint, and frontend build
make migration name=AddMeaningfulName
make clean             # Remove generated build output
```

## Manual setup

### Table of contents

- [Database](#database)
- [Backend](#backend)
- [Frontend](#frontend)
- [Environment](#environment)

### Database

From the repository root:

```bash
docker compose up -d
```

PostgreSQL is exposed on port `5433` with the development database configured in `apps/api/appsettings.Development.json`.

### Backend

```bash
cd apps/api
dotnet restore
dotnet ef database update
dotnet run
```

The API runs on port `5250`. To create a schema migration after changing backend models:

```bash
dotnet ef migrations add AddMeaningfulName
dotnet ef database update
```

Commit the migration files and model snapshot. Do not edit generated migrations manually without team agreement.

### Frontend

In another terminal:

```bash
cd apps/web
pnpm install
cp .env.example .env
pnpm dev
```

Use `VITE_API_BASE_URL=http://localhost:5250` in `.env`. Never commit `.env` or real secrets.

### Environment

The web app reads its API URL from `VITE_API_BASE_URL`. Backend development configuration is local and intentionally ignored by Git. Ask the team for development values instead of committing credentials.

## Development rules

- Use the shared frontend client in `src/lib/api/api-client.ts` and the TanStack Query provider; do not create duplicate clients.
- Keep feature API calls in that feature’s `services/`, hooks in `hooks/`, and DTO types in `types/`.
- Keep mock data isolated and remove only the mocks belonging to your feature during integration.
- Do not modify another member’s component, backend, or migration without coordination.
- Use API GUIDs for relationships; never use display labels as foreign keys.
- C# uses four-space indentation and PascalCase; TypeScript uses two spaces and the existing ESLint rules.

## Validation and pull requests

Before opening a PR:

```bash
make check
```

Use a short imperative commit message, such as `feat(fleet): connect vehicle queries`. PRs should describe the user-visible/API impact, list validation commands, identify migrations or configuration changes, and include screenshots for UI changes. Keep one feature or integration concern per PR.
