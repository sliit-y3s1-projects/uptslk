# OWASP ZAP API scan

`api-scan.yaml` is a ZAP automation plan. It imports every endpoint from the API's Swagger document, signs in as an
admin with a bearer token, runs ZAP's passive and active scans, and writes HTML, JSON and Markdown reports.

The active scan sends attack payloads and writes data. Run it only against a local API on a throwaway database.

```bash
export ZAP_TARGET=http://localhost:5250
export ZAP_REPORTS="$PWD/docs/testing/evidence/zap"
export ZAP_TOKEN=<admin JWT from POST /api/v1/auth/login>
zap.sh -cmd -autorun "$PWD/security/zap/api-scan.yaml"
```

With Docker: `docker run --network host -v "$PWD:/zap/wrk" -e ZAP_TARGET -e ZAP_REPORTS=/zap/wrk/docs/testing/evidence/zap -e ZAP_TOKEN zaproxy/zap-stable zap.sh -cmd -autorun /zap/wrk/security/zap/api-scan.yaml`

## Result on 2026-10-18 (`docs/testing/evidence/zap/before-fix/`)

ZAP 2.16.1 imported 128 URLs and the active scan ran for 3 minutes. No High or Medium risks were found.

| ID | Alert | Risk | Status |
|---|---|---|---|
| ZAP-1 | X-Content-Type-Options header missing | Low | Fixed: the API now sends `X-Content-Type-Options: nosniff` on every response, including errors. Covered by `api.Tests/Auth/SecurityHeaderTests.cs` (failed 3/3 before the fix, passes after). |
| – | Authentication request identified | Informational | Expected: login, user and driver creation take a password. |
| – | Sensitive information in URL | Informational | Passenger and trip IDs in query strings; they are GUIDs, not secrets, and every endpoint checks authorization. |
| – | User agent fuzzer | Informational | No change in behaviour by user agent. |

Limitation: ZAP fills path and query parameters with placeholder values (e.g. `tripId=tripId`), so many requests stop
at validation with HTTP 400 and do not reach deeper logic. A ZAP rescan after the fix has not been run yet.
