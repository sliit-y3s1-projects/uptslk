# Testing documentation and evidence

Use this index to locate existing reports and distinguish past evidence from new test execution.

| Resource | Purpose |
| --- | --- |
| [Web harness guide](../../apps/web/src/test/README.md) | Vitest, component rendering, API mocks, and test conventions |
| [Backend harness guide](../../apps/backend/api.Tests/README.md) | xUnit, in-process API tests, and Docker-backed database tests |
| [Mobile project guide](../../apps/mobile/README.md) | Flutter project setup |
| [Booking cases](web-bookings-test-cases.md) | Previously recorded Component D cases and outcomes |
| [Booking defects](web-bookings-defects.md) | Existing defect records |
| [Evidence directory](evidence/) | Previously saved logs, audit outputs, and TRX results |
| [Coverage summary](coverage-report/SummaryGithub.md) | Saved backend coverage report |

Reports in this directory are snapshots. A historical pass or coverage figure does not establish the result for another commit, machine, dependency graph, or environment.

## Evidence checklist

For a new run, record:

1. Commit SHA and branch, execution date/time with timezone, and operating system.
2. Runtime/package manager versions, dependency installation method, and required services.
3. Exact command, exit code, selected tests, passed/failed/skipped counts, and duration.
4. Relevant warnings, environmental failures, and scope limitations.
5. A sanitized log or a precise reference to where evidence is retained.

Mark unexecuted scenarios as planned, unavailable prerequisites as blocked, and failed assertions as failed. Do not convert warnings, skipped tests, or missing output into passing results. Avoid replacing existing evidence with another run's output under the same filename.

## Commands by layer

Run from the repository root after restoring the appropriate dependencies:

```powershell
pnpm --dir apps/web test
dotnet test apps/backend/api.Tests/api.Tests.csproj --filter "Category!=Database"
```

For Flutter, change to `apps/mobile` and run `flutter test`. For real PostgreSQL coverage, follow the backend harness guide and start Docker before selecting `Category=Database`.

Component tests with mocked API responses, in-process API tests, database tests, and manual end-to-end checks establish different things. Record which layer ran and whether external providers were mocked. Keep generated reports out of commits unless they are deliberate, reviewed evidence; remove credentials and passenger information before sharing.
