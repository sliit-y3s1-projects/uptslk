## Unified Public Transport System (UPTS)

UPTS is a full-stack public transport platform for Sri Lanka, developed as part of the SLIIT Software Engineering Framework module

Prerequisites

- Docker and Docker Compose
- .NET 8 SDK
- Node.js
- pnpm

Project Structure

- `apps/api` - ASP.NET Core Web API backend, Entity Framework Core models, and migrations
- `apps/web` - React web application for administrators and UI configuration
- `docker-compose.yml` - PostgreSQL database service configuration

Getting Started

1. Database Setup
   Ensure Docker is running, then start the PostgreSQL database container from the root directory by running:

   ```bash
   docker compose up -d
   ```

2. Backend Setup

   Navigate to the API directory and apply the database migrations to set up the schema, then start the backend server:

   ```bash
   cd apps/api
   dotnet ef database update
   dotnet run
   ```

   The `dotnet ef database update` command applies any pending migrations to your local database. Run this the first time you set up the project, and again any time you pull changes that include new migrations.

   If you make changes to the EF Core models and need to create a new migration, run the following two commands from `apps/api`:

   ```bash
   dotnet ef migrations add <MeaningfulMigrationName>
   dotnet ef database update
   ```

   Use a short, descriptive name that reflects what changed, e.g. `AddRouteScheduleTable`, `AddUserPhoneNumber`, `RenameStopToStation`. Avoid generic names like `Update1` or `Migration2` - the name is committed to the repo and should make sense to other developers reading the migration history later.

3. Frontend Setup

   Open a separate terminal window, navigate to the web directory, install the required packages, and launch the development environment:

   ```bash
   cd apps/web
   pnpm install
   pnpm dev
   ```
