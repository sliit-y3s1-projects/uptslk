using api.Entities;

namespace api.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    // Read-only lookup for the payment status page.
    Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<bool> HasSucceededForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    // The Stripe lookups below load the payment with its booking, ready to be updated.
    Task<Payment?> GetStripeForCheckoutAsync(string checkoutId, string? orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetStripeByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetStripeByCheckoutIdAsync(string checkoutId, CancellationToken cancellationToken = default);
    Task<Payment?> GetStripeByPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);
}
