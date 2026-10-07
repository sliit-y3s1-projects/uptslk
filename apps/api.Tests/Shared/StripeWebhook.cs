using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace api.Tests.Shared;

/// <summary>Builds correctly signed Stripe webhook requests so the real signature check is exercised.</summary>
internal static class StripeWebhook
{
    public static HttpRequestMessage Create(string eventId, string type, string objectJson, string? secret = null, DateTimeOffset? signedAt = null)
    {
        var header = System.Text.Json.JsonSerializer.Serialize(new { id = eventId, @object = "event", api_version = Stripe.StripeConfiguration.ApiVersion, type });
        var payload = header[..^1] + ",\"data\":{\"object\":" + objectJson + "}}";
        var timestamp = (signedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? TestApiFactory.StripeWebhookSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
        };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        return request;
    }

    public static string CheckoutSession(string checkoutId, string? paymentIntent, string orderId, long amountMinor, string currency = "lkr") =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            id = checkoutId, @object = "checkout.session", payment_intent = paymentIntent, amount_total = amountMinor, currency,
            metadata = new Dictionary<string, string> { ["upts_order_id"] = orderId }
        });
}
