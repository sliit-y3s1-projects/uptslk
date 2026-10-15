using api.DTO.Bookings;
using api.DTO.Payments;

namespace api.Interfaces.Services;

public interface IPaymentService
{
    Task<CheckoutSessionResponse> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken);
    Task<CheckoutSessionResponse> StartCheckoutForCurrentUserAsync(CreateMyBookingRequest request, CancellationToken cancellationToken);
    Task HandleStripeWebhookAsync(string json, string? signature, CancellationToken cancellationToken);
    Task<PaymentStatusResponse> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken);

    // The app link to open after Stripe sends the user back.
    Task<string> GetMobileReturnUrlAsync(string orderId, CancellationToken cancellationToken);
    string GetMobileCancelUrl(string orderId);

    Task RequestRefundAsync(Guid bookingId, RequestPaymentRefundRequest request, CancellationToken cancellationToken);
}
