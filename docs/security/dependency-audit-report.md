# Dependency audit evidence report

## Scope and provenance

This report summarizes already committed audit evidence reviewed on 2026-10-16. It does not contain a fresh dependency scan, a production assessment, or dependency changes. Findings describe the saved outputs only; their applicability to current dependencies requires another scan.

| Saved evidence | Recorded result | Interpretation |
| --- | --- | --- |
| [Full pnpm JSON](../testing/evidence/security-pnpm-audit.json) | Metadata lists 1 critical, 13 high, 18 moderate, 5 low, and 0 informational findings. | These are audit counts, not independently confirmed exploitable application defects. |
| [Production dependency output](../testing/evidence/security-pnpm-audit-prod.txt) | Contains dependency advisory entries. | A production dependency classification alone does not establish browser runtime exposure; review the dependency paths and deployment artifact. |
| [API NuGet output](../testing/evidence/security-dotnet-vulnerable-api.txt) | Reports no vulnerable packages for the sources used at scan time. | This is a historical result with a bounded advisory feed and package graph. |
| [Test project NuGet output](../testing/evidence/security-dotnet-vulnerable-tests.txt) | Lists transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.6 as high severity. | Review the test dependency graph separately from the deployed API. |

The saved pnpm JSON includes findings for packages such as `qs`, `fast-uri`, `undici`, and `dompurify`. This report neither verifies current advisory status nor asserts that the application invokes an affected code path.

## Reproduce and triage

From the repository root, with dependencies restored from the reviewed lockfile:

```powershell
pnpm --dir apps/web audit --json
pnpm --dir apps/web audit --prod
dotnet list apps/backend/api/api.csproj package --vulnerable --include-transitive
dotnet list apps/backend/api.Tests/api.Tests.csproj package --vulnerable --include-transitive
```

Record command exit codes and feed/network errors. A failed audit request is not a clean scan. Check the project paths against the branch being reviewed; older branches use `apps/api` and `apps/api.Tests`.

For each finding, capture the resolved version, dependency path, affected environment, reachable behavior, suggested update, and responsible maintainer. Review proposed upgrades in a separate PR and rerun relevant tests before describing a finding as resolved. Do not run automatic audit fixes as part of this documentation contribution.
