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

2. Identity Provider Setup

   UPTS uses ThunderID as its Identity Provider by default. Each developer runs their own local ThunderID instance using the official quickstart.

   Start ThunderID:

   ```bash
   docker compose -f oci://ghcr.io/thunder-id/thunderid-quick-start:latest up
   ```

   This automatically initializes the database, runs the setup process, and starts the ThunderID server.

   Once it's running, access the admin console at:

   `https://localhost:8090/console`

3. Backend Setup

   Navigate to the API directory to apply the database migrations and start the backend server:

   ```bash
   cd apps/api
   dotnet ef database update
   dotnet run
   ```

4. Frontend Setup

   Open a separate terminal window, navigate to the web directory, install the required packages, and launch the development environment:

   ```bash
   cd apps/web
   pnpm install
   pnpm dev
   ```
