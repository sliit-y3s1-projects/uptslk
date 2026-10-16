# Security test coverage matrix

This inventory points reviewers to existing tests on current `main`. A listed test is evidence of an implemented check, not a claim that it was executed during this documentation change.

| Concern | Existing test source under `apps/backend/api.Tests/` | Review question |
| --- | --- | --- |
| Authentication | `Auth/AuthenticationTests.cs` | Are invalid credentials and token lifecycle cases covered? |
| Centre isolation | `Auth/CentreIsolationTests.cs` | Can a user access another centre's protected records? |
| Passenger ownership and wallets | `Bookings/PassengerAccessControlTests.cs` | Are anonymous actions and access to another passenger's data denied? |
| Fare and payment permissions | `Bookings/FareAndPaymentAccessControlTests.cs` | Are money-related operations restricted to the intended roles? |
| Payment webhook handling | `Bookings/StripeWebhookTests.cs` | Are webhook validation and repeated-event cases represented? |
| Recovery operations | `AgentRecovery/RecoverySecurityTests.cs` | Are recovery actions bounded by the expected permissions? |

See the [backend test guide](../../apps/backend/api.Tests/README.md) for the test harness and environment requirements. Header-based test identities in integration tests must not be interpreted as production authentication behavior.

## Focused execution

From the repository root:

```powershell
dotnet test apps/backend/api.Tests/api.Tests.csproj --filter "FullyQualifiedName~api.Tests.Auth"
dotnet test apps/backend/api.Tests/api.Tests.csproj --filter "FullyQualifiedName~PassengerAccessControlTests|FullyQualifiedName~FareAndPaymentAccessControlTests"
dotnet test apps/backend/api.Tests/api.Tests.csproj --filter "FullyQualifiedName~StripeWebhookTests|FullyQualifiedName~RecoverySecurityTests"
```

These are suggested commands, not recorded results. Restore dependencies first. The full suite also includes PostgreSQL Testcontainers tests that require Docker; do not point tests at a shared or production database.

## Review record

For each boundary, record the permitted role, denied role, resource owner or centre, status code, and whether persistent state changed. Verify both successful authorized access and denied access; an endpoint that denies every request is not correct authorization.

Keep UI role checks separate from API enforcement. Existing web role checks in [AccessAndFareRules.test.tsx](../../apps/web/src/test/bookings/AccessAndFareRules.test.tsx) cover rendered content, while API tests inspect the server boundary. No coverage percentage or security approval is implied by this matrix.
