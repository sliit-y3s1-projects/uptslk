using Stripe;
using Stripe.Checkout;
using api.Enums;

namespace api.Services.Payments;

public sealed class StripePaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Stripe;

    public async Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken)
    {
        var secretKey = configuration["Payments:Stripe:SecretKey"];
        var webAppBaseUrl = configuration["Payments:Stripe:WebAppBaseUrl"];
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(webAppBaseUrl))
            throw new InvalidOperationException("Stripe is not configured. Set Payments:Stripe:SecretKey and Payments:Stripe:WebAppBaseUrl.");

        var baseUrl = webAppBaseUrl.TrimEnd('/');
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = request.Email,
            ClientReferenceId = request.OrderId,
            SuccessUrl = $"{baseUrl}/booking/payment-return?orderId={Uri.EscapeDataString(request.OrderId)}",
            CancelUrl = $"{baseUrl}/booking/payment-cancel?orderId={Uri.EscapeDataString(request.OrderId)}",
            Metadata = new Dictionary<string, string> { ["upts_order_id"] = request.OrderId },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = new Dictionary<string, string> { ["upts_order_id"] = request.OrderId }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = ToMinorUnits(request.Amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.Description }
                    }
                }
            }
        };

        try
        {
            var session = await new StripeClient(secretKey).V1.Checkout.Sessions.CreateAsync(options,
                new RequestOptions { IdempotencyKey = $"checkout-{request.OrderId}" }, cancellationToken);
            if (string.IsNullOrWhiteSpace(session.Url) || string.IsNullOrWhiteSpace(session.Id))
                throw new InvalidOperationException("Stripe did not return a checkout URL.");
            return new PaymentCheckoutSession(session.Url, session.Id, session.PaymentIntentId);
        }
        catch (StripeException exception)
        {
            throw new InvalidOperationException($"Stripe could not create the checkout: {exception.StripeError?.Message ?? exception.Message}");
        }
    }

    public async Task<PaymentRefundResult> RequestRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken)
    {
        var secretKey = configuration["Payments:Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey)) return new PaymentRefundResult(false, null, "Stripe is not configured.");
        try
        {
            var refund = await new StripeClient(secretKey).V1.Refunds.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = request.ProviderPaymentId,
                Amount = ToMinorUnits(request.Amount),
                Reason = "requested_by_customer"
            }, new RequestOptions { IdempotencyKey = $"refund-{request.ProviderPaymentId}" }, cancellationToken);
            return new PaymentRefundResult(true, refund.Id, null);
        }
        catch (StripeException exception)
        {
            return new PaymentRefundResult(false, null, exception.StripeError?.Message ?? exception.Message);
        }
    }

    public async Task<PaymentCheckoutStatus?> GetCheckoutStatusAsync(string providerCheckoutId, CancellationToken cancellationToken)
    {
        var secretKey = configuration["Payments:Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey)) return null;
        try
        {
            var session = await new StripeClient(secretKey).V1.Checkout.Sessions.GetAsync(providerCheckoutId, null, null, cancellationToken);
            return new PaymentCheckoutStatus(
                string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase),
                session.PaymentIntentId,
                session.AmountTotal,
                session.Currency);
        }
        catch (StripeException)
        {
            return null;
        }
    }

    private static long ToMinorUnits(decimal amount) => decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
}
