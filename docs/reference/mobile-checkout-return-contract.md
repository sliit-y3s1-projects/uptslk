# Mobile checkout and return contract

**Audience:** mobile/API integrators and payment testers.

**Baseline:** `main` commit `c2e792bfb0ec21db632e1f85815d7c1380e8a432`, reviewed 2026-10-16. This contract records the source flow; it does not report device or live Stripe validation.

## Request and checkout response

The [mobile booking client](../../apps/mobile/lib/services/booking_api_service.dart) calls `POST /api/v1/payments/checkout/me` with a Bearer token and `{ tripId, passengerCount, useMobileReturnUrl: true }`. The API resolves the caller's passenger and selects Stripe. It does not trust a mobile-supplied fare or passenger ID for that self-service route.

The response supplies `url` and `orderId`. Mobile rejects a response missing either field and opens the checkout URL in an external application. Keep the application order reference distinct from the provider checkout session ID; later payment lookup uses orderId.

Sources: [PaymentsController](../../apps/backend/api/Controllers/PaymentsController.cs), [PaymentService](../../apps/backend/api/Services/PaymentService.cs), and [BookingCheckoutPage](../../apps/mobile/lib/features/commuter/booking_checkout_page.dart).

## Return chain and configuration

The [Stripe gateway](../../apps/backend/api/Services/External/StripePaymentGateway.cs) builds these mobile browser return URLs:

```text
{MobileReturnBaseUrl}/api/v1/payments/mobile-return?orderId={encoded order}
{MobileReturnBaseUrl}/api/v1/payments/mobile-cancel?orderId={encoded order}
```

| Configuration | Implemented use |
| --- | --- |
| `Payments:Stripe:SecretKey` | Server-side provider access. |
| `Payments:Stripe:WebAppBaseUrl` | Required by checkout creation, including the mobile path. |
| `Payments:Stripe:MobileReturnBaseUrl` | Absolute HTTP/HTTPS base URL required when mobile return is requested; must reach the API from the payment browser/device. |
| `Payments:Stripe:MobileAppScheme` | Redirect scheme, default `uptslk`; an invalid scheme falls back to that default. |

The API's success-return route validates a nonempty order ID, attempts Stripe checkout reconciliation, then redirects to `uptslk://payment-return?orderId=...` with the configured/default scheme. The cancel route redirects to `uptslk://payment-cancel?orderId=...`; it does not call booking cancellation. Changing the scheme in API configuration alone does not change the installed application's registration.

Android's [manifest](../../apps/mobile/android/app/src/main/AndroidManifest.xml) and the iOS [Info.plist](../../apps/mobile/ios/Runner/Info.plist) register `uptslk`. A registered scheme makes the app a return target; it is not proof that a specific route or a cold-start checkout state will be restored. Android also disables Flutter's default deep-link routing in the reviewed manifest.

## Payment confirmation after resume

While the checkout page is alive, it retains the checkout reference and queries payment status when the app resumes, unless a check is already running or success is already known. Its explicit check also calls `GET /api/v1/payments/orders/{orderId}`. The model treats Succeeded as success and Initiated/Pending as pending.

That API lookup can reconcile a Pending Stripe checkout after a delayed webhook. A redirect, app resume, successful browser launch, or the word `payment-return` in a URL is not payment confirmation. Closing/cancelling the browser is also not evidence that a held booking has been released. Inspect payment and booking state through the server.

The reviewed checkout state lives in the widget; there is no durable order-restoration mechanism in that page. A terminated/restarted app needs separate verification. Source: [checkout state/model](../../apps/mobile/lib/models/booking_checkout.dart) and the page linked above.

## Integration verification

Check the API base is reachable from the device; device-local localhost is not the developer machine. Use provider test mode. Verify successful return, user cancellation, delayed webhook followed by status lookup, missing/invalid order, network failure on resume, mismatched scheme, and cold start separately. Record initial/final server states and actual device behavior; do not label a redirect-only test a successful payment test.

The API-side redirect and reconciliation checks are in [PaymentCheckoutTests](../../apps/backend/api.Tests/Bookings/PaymentCheckoutTests.cs). Mobile client contracts are in [booking_api_service_test.dart](../../apps/mobile/test/services/booking_api_service_test.dart). Those sources do not substitute for an executed browser-to-device handoff.
