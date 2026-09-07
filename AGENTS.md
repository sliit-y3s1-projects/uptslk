# Repository Guidelines

## Project Structure & Module Organization

UPTS is a full-stack public-transport platform. `apps/api` contains the ASP.NET Core 8 Web API: controllers, EF Core data access, domain models, DTOs, services, enums, and committed migrations. `apps/web` is the Vite/React/TypeScript administrator UI; application code lives under `src/` (`components/`, `pages/`, `context/`, `hooks/`, and `lib/`), while static files live in `public/`. PostgreSQL development infrastructure is defined in `docker-compose.yml`; product and milestone documentation is in `docs/`.

## Build, Test, and Development Commands

From the repository root, start the local database with `docker compose up -d` (PostgreSQL is exposed on port 5433). Then use separate terminals:

```bash
cd apps/api && dotnet ef database update && dotnet run
cd apps/web && pnpm install && pnpm dev
```

Run `cd apps/api && dotnet build` before submitting backend work. For the web app, run `pnpm lint` for ESLint checks and `pnpm build` for TypeScript checking plus the production Vite build. No project-owned automated test suite is currently configured; add focused tests with new behavior and document the command used to run them.

## Coding Style & Naming Conventions

Follow the existing conventions in nearby code. C# uses four-space indentation, PascalCase for public types, methods, properties, controllers, DTOs, and enum members; keep nullable annotations enabled. Put API additions in the appropriate existing layer (for example, `Models/`, `DTOs/`, then `Controllers/`). TypeScript and TSX use two-space indentation, double quotes, and PascalCase component filenames such as `DashboardPage.tsx`; hooks use `useX.ts`. Prefer the `@/` import alias for web source imports. Run the configured ESLint rules instead of manually reformatting generated UI components.

## Database & Configuration

Treat schema changes as team-visible work. Discuss them first, then create descriptive migrations from `apps/api`, e.g. `dotnet ef migrations add AddRouteScheduleTable`, and commit both migration files and the model snapshot. Do not commit real secrets; keep development credentials in local configuration. The API migrates the database on startup, so review migration effects carefully.

## Commits & Pull Requests

Recent history favors short imperative summaries, with occasional scoped prefixes such as `docs: add component breakdown and milestone`. Use similarly concise messages that name the change (`fix linting errors`, `docs: clarify setup`). Keep each commit focused. Pull requests should explain the user-visible or API/schema impact, link the relevant issue or milestone item, list validation commands run, and include screenshots for UI changes. Flag migrations, configuration changes, and any required setup steps explicitly.
