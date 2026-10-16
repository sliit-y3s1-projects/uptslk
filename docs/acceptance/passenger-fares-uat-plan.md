# Passenger and fares acceptance test plan

**Audience:** Component D tester and reviewer.

**Baseline:** `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, source reviewed 2026-10-16.

**Status:** planned manual acceptance work; no case below is marked executed. Use the accompanying [execution record](passenger-fares-uat-record.md) for actual outcomes.

## Objective and environment

Check that passenger management, group booking, money movement, and ticket presentation agree with persisted API state. Use a local database, synthetic passengers, future-dated trips, and provider test mode. UI routes are documented in the [feature guide](../../apps/web/src/features/fares/README.md); API rules come from [BookingService](../../apps/backend/api/Services/BookingService.cs), [BookingCancellationService](../../apps/backend/api/Services/BookingCancellationService.cs), and [BookingPaymentService](../../apps/backend/api/Services/BookingPaymentService.cs).

Prepare one Admin/CentreManager for fare setup, one Dispatcher for booking/wallet actions, and two Commuter accounts linked to separate passengers. Prepare a Scheduled future trip with capacity 5 and an active LKR 250 standard fare. Use a synthetic wallet balance of LKR 1,000 for the wallet cases. Use separate bookings or resettable isolated fixtures for independent cases; retain financial history rather than deleting rows to force a desired result.

Record actual GUIDs and the departure timestamp in the private execution evidence. Do not use static dates from this document: an expired fixture changes the eligibility result. For independent tests, return to the stated starting balance/capacity through a new fixture, not by silently altering the expected values.

## Acceptance cases

| ID / priority | Preconditions and steps | Expected observable result |
| --- | --- | --- |
| D-UAT-01 / High | As Dispatcher, create a synthetic passenger without portal credentials; open detail and reload. | Profile persists with a wallet at zero; no portal account is implied. |
| D-UAT-02 / High | Top up that wallet by LKR 1,000; reload passenger detail. | Balance is 1,000 and exactly one Topup transaction describes the action. |
| D-UAT-03 / High | With balance 1,000, fare 250, and capacity 5 available, create a wallet booking for 3. | Booking is Confirmed, count 3, total fare 750; balance becomes 250; occupied capacity increases by 3. |
| D-UAT-04 / High | After D-UAT-03, attempt another booking for 3 on the same trip, with an independently funded passenger. | Capacity conflict is reported; no extra booking debit is committed. Remaining capacity remains 2. |
| D-UAT-05 / High | Cancel the D-UAT-03 booking with a reason through the staff flow, reload, then repeat the request once. | First cancellation returns 750 to the original wallet and records a Refund; repeat is rejected and does not credit again. |
| D-UAT-06 / High | With a fresh trip/fixture, start card checkout for 2 passengers without paying. | Pending booking holds 2 spaces; wallet balance does not decrease. |
| D-UAT-07 / High | Complete D-UAT-06 with the provider's test payment workflow, then query order status/reload. | Matching payment becomes Succeeded; Pending booking becomes Confirmed. Record whether webhook or status reconciliation established the result. |
| D-UAT-08 / High | Request a refund for that paid card booking; inspect application and provider test evidence. | On acceptance: booking Cancelled, payment RefundPending, no wallet credit. Mark refund completion separately after a succeeded provider update. |
| D-UAT-09 / High | As Commuter A, use `/bookings/me`; repeat as Commuter B with a different linked profile. | Each response contains only that caller's tickets. Record identities privately and compare booking IDs. |
| D-UAT-10 / Medium | Use an isolated trip at/past scheduled departure, then attempt new booking. | Creation is rejected. Check returned CanBoard/QR separately; Boarding/Delayed tickets have an active-window rule. |
| D-UAT-11 / High | As Commuter, attempt a staff-only fare modification through the API. | Request is denied and the fare remains unchanged. A hidden UI button alone is insufficient evidence. |
| D-UAT-12 / Medium | Deactivate the trip route's standard fare, attempt booking, then restore that existing rule. | Booking fails without an active fare; restoration uses the original fare record instead of creating a duplicate. |

## Evidence requirements

Capture the action, role, initial state, request method/path, response status, final state after reload, and sanitized transaction/provider evidence where relevant. For balance/capacity cases, record the numbers before and after; a toast or screenshot by itself does not establish persistence or financial correctness.

Automated component checks can support the review, but mocked responses do not execute a live provider or database workflow. Relevant sources include [BookingApiTests](../../apps/backend/api.Tests/Bookings/BookingApiTests.cs), [PaymentRefundTests](../../apps/backend/api.Tests/Bookings/PaymentRefundTests.cs), and [CommuterBookingTests](../../apps/backend/api.Tests/Bookings/CommuterBookingTests.cs).

## Exit criteria and defects

Acceptance is complete when every case has a recorded outcome, all High-priority cases pass, and remaining failures/blocked cases have an owner and an explicit reviewer disposition. If Stripe test access is unavailable, mark provider cases Blocked and keep that limitation visible; passing wallet tests cannot substitute for them. This is a proposed acceptance gate, not a claim of team sign-off.

For a defect, retain case ID, reproducible steps, expected/actual state, environment/commit, severity rationale, and sanitized evidence. Re-run the affected case after a fix and link the new result rather than overwriting the original failure. Record any untested mobile or live-provider behavior explicitly.
