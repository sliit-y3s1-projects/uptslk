# Booking regression review

This guide connects existing web tests to repeatable review steps. It adds test scenarios without changing application or test source files.

## Run existing automated checks

From the repository root:

```powershell
pnpm --dir apps/web test src/test/bookings
pnpm --dir apps/web test src/test/bookings/BookingCancellation.test.tsx
```

Install the web dependencies from the committed lockfile if needed. The [test harness](../../apps/web/src/test/README.md) uses jsdom and mocked API requests. These tests exercise components and request contracts; they do not call a real payment service or prove database persistence.

## Verification recorded for this guide

On 2026-10-16 (Asia/Colombo), the following command ran against `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432` in a separate worktree on Windows, with Node.js v22.19.0 and pnpm 11.15.0:

```powershell
pnpm --dir apps/web exec vitest run src/test/bookings --coverage.enabled=false --cache=false
```

Result: exit code 0; 9 test files passed, 39 tests passed; duration 10.89 seconds. The run printed `Not implemented: navigation to another Document` from jsdom. Coverage was disabled, and no API, database, mobile, or payment-provider tests were run. Dependencies were installed from the existing lockfile; no tracked application or lockfile changes resulted.

## Cancellation coverage and remaining checks

The existing [cancellation test](../../apps/web/src/test/bookings/BookingCancellation.test.tsx) checks that confirmation is disabled without a reason, enabled after entering a reason, sends that reason in a DELETE request, and shows success feedback.

The following additional manual cases are **planned, not executed by this document**. Use a local test account and test booking; never cancel a real passenger booking.

| ID | Action | Expected evidence |
| --- | --- | --- |
| CANCEL-01 | Open cancellation and leave the reason empty. | Confirmation remains disabled and no cancellation request is sent. |
| CANCEL-02 | Cancel an eligible test booking with a reason. | Request includes the reason; subsequent booking and wallet reads agree with the server result. |
| CANCEL-03 | Simulate a failed cancellation response in a controlled test environment. | The UI reports the failure and does not report a successful refund. |
| CANCEL-04 | Attempt cancellation as another passenger through the API. | Access is denied and booking/wallet state remains unchanged. |
| CANCEL-05 | Repeat the same cancellation request. | No second refund is credited; record the actual status and resulting balance. |

For each execution, record the commit, account role, synthetic booking ID, initial balance, expected result, actual response, final balance, and pass/fail/blocked status. Redact authentication headers and personal data.

Compare current output with the historical [booking case report](web-bookings-test-cases.md), but do not copy historical pass counts into a new run. Review payment, authorization, and concurrency behavior through API tests or an isolated integration environment.
