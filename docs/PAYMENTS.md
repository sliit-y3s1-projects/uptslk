# Stripe payments

UPTS uses Stripe Checkout for commuter bookings. The API owns fare calculation, creates the Stripe Checkout Session, and confirms a booking only when it receives Stripe's signed webhook. The React app never receives a Stripe secret key and only redirects the browser to the Checkout URL returned by the API.

## Configuration

Put test credentials in `apps/api/appsettings.Development.json` for this university project. Do not commit live keys.

```json
"Payments": {
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "WebAppBaseUrl": "http://localhost:5173"
  }
}
```

`SecretKey` comes from Stripe Workbench/API keys. `WebhookSecret` is provided by the Stripe CLI listener below. The frontend needs neither value.

## Local browser test

1. Apply the migration and start the API:

   ```bash
   cd apps/api
   dotnet ef database update
   dotnet run
   ```

2. In another terminal, start the web app:

   ```bash
   cd apps/web
   pnpm dev
   ```

3. In a third terminal, sign in to Stripe CLI and forward signed webhooks to the API—no ngrok is needed:

   ```bash
   stripe login
   stripe listen --forward-to http://localhost:5250/api/v1/payments/stripe/webhook
   ```

   Copy the displayed `whsec_...` value into `Payments:Stripe:WebhookSecret`, then restart the API.

4. Open `http://localhost:5173`, sign in as a commuter, choose a route, departure, and seat, then select **Continue to secure payment**. The browser redirects to Stripe Checkout.

5. In test mode, use Stripe's successful test card `4242 4242 4242 4242`, any future expiry, any CVC, and any postal code. After payment Stripe sends `checkout.session.completed`; the return page polls the API until the booking is confirmed. As a recovery measure, that status request also verifies the Checkout Session directly with Stripe; it never trusts the browser return alone.

## Events handled

- `checkout.session.completed`: validates order, amount, and currency, then confirms the booking.
- `payment_intent.payment_failed` and `checkout.session.expired`: fail/cancel an unpaid booking and release its seat.
- `refund.updated`: settles a requested booking refund.

Webhook event IDs are stored uniquely so Stripe retries do not duplicate booking or refund work. Refunds are requested through the API and only become settled when Stripe reports the result.
