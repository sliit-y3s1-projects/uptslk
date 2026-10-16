# Checkout and refund failure runbook

**Audience:** maintainers supporting local test checkout and cancellation.

**Source baseline:** `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16.

**Use with:** [payment reconciliation](payment-reconciliation.md). Preserve the original financial records and investigate the payment path before issuing another money-moving request.

## Symptom-to-action table

| Symptom | Implemented behavior to inspect | Next action |
| --- | --- | --- |
| Checkout cannot start because provider configuration is unavailable | The `InvalidOperationException` handling branch marks payment Failed and booking Cancelled, then returns Service Unavailable. | Check local provider configuration and verify held capacity was released before starting a new checkout. Other exception types must be investigated separately. |
| Browser/app returns but payment still appears Pending | Order lookup can reconcile a paid matching Stripe session. | Query status with the order reference and follow the reconciliation procedure; retain the before/after states. |
| Checkout expires | A matching expiry event cancels payment and booking unless payment already Succeeded. | Confirm event delivery and capacity change; do not assume closing a browser sent an expiry event. |
| Webhook rejected | Missing signature, failed verification, or missing webhook configuration has a distinct error path. | Check the matching environment's webhook configuration and original request delivery. Preserve the signed payload when using provider tooling; do not hand-edit it. |
| Provider rejects the refund immediately | A Failed refund attempt is saved; the call returns Bad Gateway, and the booking remains active. | Inspect the recorded failure and provider state. A later retry requires an active booking and Succeeded payment. |
| Refund accepted, then fails asynchronously | `refund.updated` sets the refund Failed and payment back to Succeeded; it does not reactivate the Cancelled booking. | Escalate to a maintainer. The standard refund entry point rejects Cancelled bookings, so a simple retry through that endpoint will not resolve this case. |
| Trip cancellation reports unrefunded card bookings | Trip cancellation commits the trip/wallet phase before attempting card refunds separately. | Retain the returned unrefunded booking IDs and review each card case; trip cancellation is not proof that all card refunds finished. |

Sources: [BookingPaymentService](../../apps/backend/api/Services/BookingPaymentService.cs), [PaymentService](../../apps/backend/api/Services/PaymentService.cs), and [BookingCancellationService](../../apps/backend/api/Services/BookingCancellationService.cs).

## Refund decision record

Before a retry, record booking status, latest successful payment, previous refund status, provider refund reference, and whether the provider accepted a prior request. A timeout or lost response is an unknown outcome until checked; it is not evidence that the provider did nothing.

An accepted card refund changes the payment to RefundPending and cancels the booking immediately. A later succeeded event changes the payment to Refunded. The booking's `RefundAmount` is set on acceptance and is therefore not sufficient proof of provider completion.

Wallet cancellation follows another path: it locks/reloads the booking and wallet, credits the fare, and records a Refund transaction. A booking with checkout history but no successful payment is cancelled without a wallet credit. Check the transaction history rather than inferring the refund destination from a generic UI success message.

## Late and repeated events

The service checks stored provider event IDs and already-successful payment states in several handlers. Do not generalize these checks into a guarantee for all concurrency or provider ordering cases. If a success arrives after cancellation, payment state can become Succeeded while the booking remains Cancelled; inspect and escalate that combination instead of creating a second booking automatically.

Do not repair a discrepancy by manually changing balances, deleting transactions, or flipping booking/payment statuses. Such edits bypass service invariants and erase useful evidence. Any corrective implementation or data repair needs a separately reviewed scope with a verified provider outcome.

## Handoff and verification

Attach a sanitized timeline, application commit, synthetic booking/order/event IDs, exact observed responses, provider result, previous retry count, and current capacity/balance impact. State whether the issue is configuration, delivery, rejection, unknown outcome, or an implementation limitation. The receiving maintainer should record the next owner/action; no response deadline is implied here.

Relevant regression sources: [PaymentRefundTests](../../apps/backend/api.Tests/Bookings/PaymentRefundTests.cs), [StripeWebhookTests](../../apps/backend/api.Tests/Bookings/StripeWebhookTests.cs), and [PaymentConcurrencyTests](../../apps/backend/api.Tests/Database/PaymentConcurrencyTests.cs). This runbook was reviewed against source, not exercised against a live provider.
