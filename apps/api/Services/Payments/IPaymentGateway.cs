using api.Enums;

namespace api.Services.Payments;

public sealed record PaymentCheckoutRequest(
    string OrderId, decimal Amount, string Currency, string FirstName, string LastName,
    string Email, string Phone, string Description);

public sealed record PaymentCheckoutSession(string Url, string ProviderCheckoutId, string? ProviderPaymentId = null);

public sealed record PaymentCheckoutStatus(bool IsPaid, string? ProviderPaymentId, long? AmountMinor, string? Currency);

public sealed record PaymentRefundRequest(string ProviderPaymentId, decimal Amount, string Reason);

public sealed record PaymentRefundResult(bool IsAccepted, string? ProviderRefundId, string? FailureReason);

public interface IPaymentGateway
{
    PaymentProvider Provider { get; }
    Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken);
    Task<PaymentCheckoutStatus?> GetCheckoutStatusAsync(string providerCheckoutId, CancellationToken cancellationToken);
    Task<PaymentRefundResult> RequestRefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken);
}
