# api.Tests – backend test suite

Run everything: `cd apps && dotnet test api.Tests/api.Tests.csproj`
Single area: `dotnet test api.Tests/api.Tests.csproj --filter "FullyQualifiedName~api.Tests.Fleet"`
Coverage: `dotnet test api.Tests/api.Tests.csproj --settings api.Tests/coverage.runsettings --results-directory api.Tests/TestResults/cov` then `reportgenerator` (report committed in `docs/testing/coverage-report/`).
Results (TRX): `dotnet test api.Tests/api.Tests.csproj --logger "trx;LogFileName=results.trx"` → `api.Tests/TestResults/`

Run without Docker: `dotnet test api.Tests/api.Tests.csproj --filter "Category!=Database"`; database tests only: `--filter "Category=Database"`.

Tests (except `Database/`) run in-process against the real API (`WebApplicationFactory`) with an in-memory SQLite database and a
header-based test authentication handler (`X-Test-Role`, `X-Test-User-Id`, `X-Test-Centre-Id`).

| Folder | Component | Owner | Covers |
| --- | --- | --- | --- |
| `Centres/` | A – Centres and Network | kishan-ahamed45 | Centre/bay/route CRUD, stops, timetables, access control |
| `Fleet/` | B – Fleet and Maintenance | RashmiK0119 | Vehicle CRUD, maintenance records, access control |
| `Dispatch/` | C – Scheduling and Dispatch | chamals3n4 | Trip CRUD, conflicts, status lifecycle, driver flow, access control |
| `Bookings/` | D – Passengers and Fares | Nadeesha-D-Shalom | Booking/wallet/fare, passengers, commuter `/me` endpoints, access control |
| `AgentRecovery/` | Shared agent workflow | whole team | Validator, composer, planner, tools, security, HTTP |
| `Database/` | Database testing (all components) | whole team | Real PostgreSQL 17 through Testcontainers: migrations, constraints, relationships, transactions, booking concurrency. **Needs Docker.** |
| `Auth/` | Shared foundation | whole team | Real JWT login/refresh/logout, account and admin user management, centre-to-centre isolation |
| `Shared/` | Harness | whole team | `TestApiFactory`, `TestDb`, `TestApiExtensions`, `TestWorld` seed data |

Each member should add further tests for their own entity in their own folder and commit them under their own account.
