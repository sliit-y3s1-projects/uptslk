# Local development troubleshooting

**Audience:** contributors diagnosing the local development stack.

**Source baseline:** `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16.

**Scope:** source-checked diagnostic commands and interpretation. No database reset, migration, provider transaction, or environment configuration was executed to validate this guide.

## Confirm the branch layout first

Current `main` places .NET projects in `apps/backend/api` and `apps/backend/api.Tests`. Earlier branches use `apps/api` and `apps/api.Tests`. A project-not-found error can therefore be a branch/path mismatch. From the repository root:

```powershell
git branch --show-current
git rev-parse HEAD
Test-Path apps/backend/api/api.csproj
Test-Path apps/backend/api.Tests/api.Tests.csproj
```

Follow the layout on your branch; do not copy/move application folders to make an old command work. The current [Makefile](../../Makefile) sets `BACKEND_DIR := apps/backend` and requires Bash, so Windows contributors without Bash/Make can use the explicit commands below.

## Database and startup diagnosis

```powershell
docker compose ps
docker compose logs --tail 80 postgres
Test-NetConnection localhost -Port 5433
```

The committed [Compose configuration](../../docker-compose.yml) maps host port 5433 to PostgreSQL's container port 5432 and stores data in a named volume. An API running on the host must use the host port in its local connection string. A successful TCP check establishes a reachable port, not correct database credentials or schema.

Review local `ConnectionStrings:DefaultConnection` without printing secrets into shared logs. Check the database is healthy before investigating application queries. Do not remove the database volume as a troubleshooting shortcut; it contains persisted local data.

[Program](../../apps/backend/api/Program.cs) initializes the database outside the Testing environment; [WebApplicationExtensions](../../apps/backend/api/Common/Extensions/WebApplicationExtensions.cs) applies migrations and seeds the initial administrator before serving requests. Starting the API can therefore change the selected database. Confirm it is the intended local database first. If migration fails, retain the exception and migration name and resolve the cause with the team; do not invent a replacement migration or edit the model snapshot to silence the error.

## API and web diagnosis

For a configured local environment, start the API and web in separate terminals from the repository root:

```powershell
dotnet run --project apps/backend/api/api.csproj
```

```powershell
pnpm --dir apps/web dev
```

The API defaults to port 5250 unless `ASPNETCORE_URLS` overrides it. Vite commonly uses 5173; read its actual startup output. With the default API origin:

```powershell
Invoke-WebRequest -Uri http://localhost:5250/health
```

The health route includes a database check. A healthy result does not establish Stripe, storage, AI, authentication, or mobile correctness. Inspect API logs if the request is redirected, refused, or unhealthy.

| Symptom | Check | Interpretation / next step |
| --- | --- | --- |
| Browser cannot reach API | Actual API listener and web `VITE_API_BASE_URL` | Align the client origin with the running API, then restart Vite after an environment change. |
| Browser reports CORS failure | Exact web scheme/host/port | The policy includes localhost 5173/4173, the configured Stripe WebAppBaseUrl, and the committed hosted origin. `127.0.0.1` or another port is a different origin. Inspect policy/configuration before proposing a change. |
| API returns 401 | Session/token validity and correct environment | Authentication failed; do not solve it by changing role checks. |
| API returns 403 | Authenticated role and applicable centre rights | Compare the caller with the endpoint's declared authorization and service ownership rules. |
| Booking rejected despite visible trip | Departure UTC, status, active fare/passenger, count, funds/capacity | The server rechecks business rules; a rendered trip is not a reservation. |
| Paid checkout still appears pending | Order reference and webhook/status evidence | Use the existing payment service reconciliation path; a return URL is not proof of payment. |

Sources: [API/CORS setup](../../apps/backend/api/Common/Extensions/ApiServiceExtensions.cs), [database setup](../../apps/backend/api/Common/Extensions/DatabaseServiceExtensions.cs), and [web feature integration](../../apps/web/src/features/fares/README.md).

## Separate test failures from service failures

The web component harness uses mocked HTTP responses and does not need a live API for its normal cases. Backend non-Database tests use the in-process harness; real PostgreSQL concurrency/migration tests require Docker. Read the [web test guide](../../apps/web/src/test/README.md) and [backend test guide](../../apps/backend/api.Tests/README.md) before treating an unavailable provider/database as an application regression.

For a useful handoff, include commit, command, exact sanitized error, actual origins, runtime versions, service health, and the smallest reproducible step. State which commands were actually run and leave unresolved observations visible. Do not attach local settings files or authentication headers.
