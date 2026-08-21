### Unified Public Transport System (UPTS)

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
   Navigate to the API directory to apply the database migrations and start the backend server:

   ```bash
   cd apps/api
   dotnet ef database update
   dotnet run
   ```

3. Frontend Setup
   Open a separate terminal window, navigate to the web directory, install the required packages, and launch the development environment:
   ```bash
   cd apps/web
   pnpm install
   pnpm dev
   ```
