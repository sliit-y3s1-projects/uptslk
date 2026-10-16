# Standard fare rules and calculations

**Audience:** staff-workflow reviewers and booking testers.

**Source baseline:** `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16.

**Purpose:** prevent confusion between a per-passenger quote, a group's stored booking fare, and historical category-based assumptions.

## Rule management

The current FareRule stores RouteId, Amount, and IsActive. It does not contain a passenger category. The [database mapping](../../apps/backend/api/Data/Configurations/FareRuleConfiguration.cs) permits one fare record per route, regardless of whether it is active.

Creation requires an active route and checks the caller's right to manage that route's centre. If any standard fare already exists for the route, creation returns Conflict and instructs the caller to edit it. To restore an inactive rule, update the existing record; deactivation does not free the route for a second fare record.

Create/update amounts are validated from 0.01 through 1,000,000 inclusive by [FareRuleRequests](../../apps/backend/api/DTO/Fares/FareRuleRequests.cs). The stored amount uses two decimal places. This validation range is an input constraint, not a business recommendation for actual transport pricing.

| Operation under `/api/v1/fare-rules` | Declared role requirement | Additional service check |
| --- | --- | --- |
| `POST` | Admin or CentreManager | Active route; manageable centre; no existing route fare. |
| `PUT /{fareRuleId}` | Admin or CentreManager | Rule exists; caller can manage the rule's route centre. |
| `DELETE /{fareRuleId}` | Admin or CentreManager | Same management check; sets IsActive false. |
| `GET /quote/me?tripId=...` | Commuter | Resolves an active passenger profile for the caller. |

Sources: [FareRulesController](../../apps/backend/api/Controllers/FareRulesController.cs), [FareRuleService](../../apps/backend/api/Services/FareRuleService.cs), and [AppRoles](../../apps/backend/api/Common/Constants/AppRoles.cs).

## Quote versus booking total

`QuoteAsync` requires an existing trip, an active passenger, and an active route fare. It returns the rule's Amount together with trip/passenger context. It does not multiply by a passenger count or reserve capacity. A quote is not proof that a departure remains bookable or that funds/capacity will be available at submission time.

Both wallet booking and external checkout calculate the stored booking fare as:

```text
booking.Fare = active route fare.Amount × request.PassengerCount
```

| Synthetic example | Per-passenger amount | Passenger count | Booking total |
| --- | --- | --- | --- |
| Single passenger | LKR 250.00 | 1 | LKR 250.00 |
| Group booking | LKR 250.00 | 3 | LKR 750.00 |
| Maximum accepted group size | LKR 250.00 | 10 | LKR 2,500.00 |

These are explanatory examples, not real service fares. Passenger category is copied into the booking snapshot, but the current standard-fare selection is by route. Do not infer student/senior discounts from the presence of a category field.

The wallet service debits the calculated total. External checkout records the same total in the Payment amount. Stripe reconciliation compares amount in minor units (`Amount × 100`) and currency. Source: [BookingService](../../apps/backend/api/Services/BookingService.cs) and [BookingPaymentService](../../apps/backend/api/Services/BookingPaymentService.cs).

## Change review checklist

When reviewing a fare edit, record the route ID, previous/new amount, active flag, managing role/centre, and execution time. Verify new bookings use the expected active rule and previously stored booking totals remain historical values. Check rejection of zero/negative input, duplicate route fare creation, and booking without an active fare. Do not describe quote success as a booking guarantee.

Relevant source tests are [FareRuleApiTests](../../apps/backend/api.Tests/Bookings/FareRuleApiTests.cs) and [CommuterBookingTests](../../apps/backend/api.Tests/Bookings/CommuterBookingTests.cs). This reference records source behavior; it introduces no fare changes and does not report a fresh test run. Field relationships are summarized in the accompanying [data dictionary](passenger-fares-data-dictionary.md).
