using api.DTO.Payments;

namespace api.Interfaces.Services;

// Card payments for bookings: starting a checkout, handling the provider's updates, and refunds.
public interface IBookingPaymentService
{
    // Holds the seats with a pending booking and opens a checkout page with the provider.
    Task<PaymentCheckoutSession> StartCheckoutAsync(StartCheckoutRequest request, CancellationToken cancellationToken);

    Task HandleStripeCheckoutCompletedAsync(string eventId, string checkoutId, string? paymentIntentId, string? orderId, long? amountTotal, string? currency, CancellationToken cancellationToken);
    Task HandleStripePaymentFailedAsync(string eventId, string paymentIntentId, CancellationToken cancellationToken);
    Task HandleStripeCheckoutExpiredAsync(string eventId, string checkoutId, CancellationToken cancellationToken);
    Task HandleStripeRefundUpdatedAsync(string eventId, string refundId, string? status, CancellationToken cancellationToken);

    // Asks Stripe for the checkout result, in case its webhook has not arrived yet.
    Task ReconcileStripeCheckoutAsync(string orderId, CancellationToken cancellationToken);

    // Cancels the booking and asks the provider to refund the card.
    Task RequestRefundAsync(Guid bookingId, string reason, CancellationToken cancellationToken);
}
