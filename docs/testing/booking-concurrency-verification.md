# Booking and payment concurrency verification

**Audience:** maintainers reviewing money/capacity invariants under parallel requests.

**Baseline:** `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16. This is a verification procedure for existing tests; it contains no new PostgreSQL execution results.

## Why a separate database run matters

[DbContextLockExtensions](../../apps/backend/api/Data/DbContextLockExtensions.cs) uses PostgreSQL `SELECT ... FOR UPDATE` and takes no row lock for a non-Npgsql provider. SQLite-based API tests can verify normal service outcomes but do not exercise the production provider's row-lock behavior. A passing component or SQLite test is insufficient evidence for parallel PostgreSQL safety.

The existing [architecture guide](../../apps/backend/api/docs/ARCHITECTURE.md) owns the locking design. This document focuses on which invariants to observe and how to record a database test run; it does not propose a different lock order or migration.

## Invariants and existing cases

| Invariant | Existing source / case | Evidence to inspect |
| --- | --- | --- |
| Occupancy does not exceed vehicle capacity | BookingConcurrencyTests: `ParallelBookings_NeverExceedVehicleCapacity` | Sum PassengerCount for Pending/Confirmed bookings; compare with configured capacity. |
| One wallet cannot fund more than its balance | BookingConcurrencyTests: `ParallelBookings_ByOneWallet_CannotSpendMoreThanTheBalance` | Final balance is nonnegative; successful booking count agrees with available funds. |
| Repeated wallet cancellation refunds once | BookingConcurrencyTests: `ParallelCancellations_RefundTheBookingOnlyOnce` | Final wallet and exactly one Refund transaction. |
| Pending external checkouts cannot oversell | PaymentConcurrencyTests: `ParallelCheckouts_NeverHoldMoreSeatsThanTheVehicleHas` | Held passenger capacity across pending/confirmed bookings. |
| Concurrent refund requests do not duplicate the provider call | PaymentConcurrencyTests: `ParallelRefundRequests_ReachStripeOnlyOnce` | Fake gateway call count and persisted payment/refund state. |
| Trip/booking cancellation races do not double-refund | PaymentConcurrencyTests: `ParallelTripAndBookingCancellations_RefundThePassengerOnlyOnce` | Balance, refund history, and cancellation outcomes. |
| Booking versus trip cancellation preserves the intended final state | PaymentConcurrencyTests: `BookingWhileTheTripIsBeingCancelled_NeverLeavesAPaidBookingOnACancelledTrip` | Final trip/booking states and related money records. |

Sources: [BookingConcurrencyTests](../../apps/backend/api.Tests/Database/BookingConcurrencyTests.cs) and [PaymentConcurrencyTests](../../apps/backend/api.Tests/Database/PaymentConcurrencyTests.cs). Passing these finite scenarios is bounded regression evidence, not proof of every possible interleaving or live provider behavior.

## Prerequisites and execution

Use the .NET SDK required by the project and an available Docker engine. The [PostgresFixture](../../apps/backend/api.Tests/Database/PostgresFixture.cs) starts PostgreSQL 17 through Testcontainers, applies migrations to a template database, and clones an isolated database for each test. The fixture chooses its container connection settings; it does not use the regular development Compose database as its test target.

From the repository root, after restoring packages:

```powershell
docker info
dotnet test apps/backend/api.Tests/api.Tests.csproj --filter "FullyQualifiedName~api.Tests.Database.BookingConcurrencyTests|FullyQualifiedName~api.Tests.Database.PaymentConcurrencyTests" --logger "console;verbosity=normal"
```

For all database tests, the [backend test guide](../../apps/backend/api.Tests/README.md) documents `Category=Database`. Do not silently substitute a SQLite run when Docker is unavailable. Record that prerequisite as blocked. An image-pull/startup failure is an environment failure, not a passing concurrency result.

## Evidence and failure triage

Record commit, SDK/OS, PostgreSQL image, exact command/filter, exit code, total/passed/failed/skipped counts, and sanitized failure output. Check that the named classes actually executed; a filter matching zero tests establishes nothing.

When a case fails, distinguish fixture startup/migration errors, request errors, and invariant violations. Preserve the first failure and relevant state before changing code. Include configured capacity, group counts, starting/final balance, booking statuses, transaction counts, and provider call counts where relevant. Do not repair a test by weakening its assertion or editing financial state.

Repeat after a targeted fix or an unresolved intermittent failure, and retain both outcomes. Provider operations in these tests use the test gateway; live Stripe concurrency and network-unknown outcomes require a separately scoped integration review. No claim of provider exactly-once delivery is made here.
