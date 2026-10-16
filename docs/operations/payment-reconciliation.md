# Payment reconciliation procedure

**Audience:** Component D maintainers investigating a test payment.

**Source baseline:** `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16.

**Scope:** reconcile one application's booking/payment record with Stripe test-mode evidence. This is not a settlement, accounting, or production operations procedure.

## Identify the records before taking action

| Identifier | Meaning | Typical use |
| --- | --- | --- |
| Booking ID | Application booking GUID | Open booking detail and identify its passenger/trip. |
| `ProviderOrderId` | Application-generated `UPTS-...` reference | Query application payment status and correlate provider metadata. |
| `ProviderCheckoutId` | Provider checkout session reference | Compare the application checkout with provider session evidence. |
| `ProviderPaymentId` | Provider payment reference | Trace successful payment and refund requests. |
| `ProviderRefundId` | Provider refund reference | Follow asynchronous refund updates. |
| `ProviderEventId` | Webhook event reference | Identify processing/replay evidence separately from an order. |

These identifiers are not interchangeable. Collect synthetic identifiers and times through authorized application views or a maintainer's controlled inspection. Do not include authentication headers, passenger details, or provider keys in public PR evidence.

## Procedure

1. Record the reviewed commit, environment, UTC timestamps, booking ID, order ID, and the symptom reported by the tester. Check the provider account is in test mode and belongs to the same environment.
2. Inspect the booking and identify its funding path. A wallet-funded booking has wallet Fare/Refund transactions; it does not require a Stripe payment record. Avoid treating a wallet debit as a missing card payment.
3. For external checkout, query `GET /api/v1/payments/orders/{orderId}` using the application's existing session/client. **This read can change state:** `PaymentService.GetByOrderIdAsync` first attempts reconciliation of a Pending Stripe checkout.
4. Compare the application's amount/currency with the provider session. The implementation matches minor units using `payment.Amount × 100` and compares currency without case sensitivity. An unmatched or unpaid session does not confirm the booking through reconciliation.
5. Re-read the application result. A paid, matching Pending checkout can become Succeeded, and a Pending booking can become Confirmed. An unknown order returns Not Found. A return-page redirect alone is insufficient payment evidence.
6. If the records disagree, capture their states and timestamps before retrying any mutation. Use the [failure runbook](payment-failure-runbook.md) to classify the case. Escalate unresolved differences to the maintainer with the sanitized evidence bundle below.

Sources: [PaymentService](../../apps/backend/api/Services/PaymentService.cs), [BookingPaymentService](../../apps/backend/api/Services/BookingPaymentService.cs), and [PaymentStatusResponse](../../apps/backend/api/DTO/Payments/PaymentResponses.cs).

## Expected observations by path

| Path | Application evidence | What it does not prove |
| --- | --- | --- |
| Wallet booking | Confirmed booking, wallet balance reduced by total fare, Fare transaction | Any card charge occurred. |
| Pending card checkout | Pending booking, payment Initiated/Pending, held capacity | That the provider has received payment. |
| Successful card checkout | Payment Succeeded; booking confirmed if still Pending | That a previously cancelled booking was reactivated. |
| Accepted card refund | Booking Cancelled, payment RefundPending, refund Requested | That funds have already reached the cardholder. |
| Completed refund event | Refund Succeeded and payment Refunded | That the wallet received a refund; this path returns to the provider. |

The order-status response contains booking ID, payment status, amount, currency, provider, and update time. It does not expose a refund history or settlement report. There is no need to invent a reconciliation endpoint for this procedure.

## Evidence and closure

Record initial and final booking/payment/refund states, amount/currency, relevant provider event references, application response status, and whether querying status triggered reconciliation. Close the investigation only when both sources agree or a maintainer records a tracked discrepancy and next action. Keep a pending refund explicitly pending.

Existing automated checks include `OrderStatus_ReconcilesAPaidCheckoutWithStripe` in [PaymentCheckoutTests](../../apps/backend/api.Tests/Bookings/PaymentCheckoutTests.cs) and the signature/replay checks in [StripeWebhookTests](../../apps/backend/api.Tests/Bookings/StripeWebhookTests.cs). Those tests use the repository harness; this document does not record a new live Stripe run. For initial provider setup, use the [Stripe setup guide](../STRIPE_SETUP_GUIDE.md).
