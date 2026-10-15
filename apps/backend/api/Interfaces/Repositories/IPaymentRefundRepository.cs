using api.Entities;

namespace api.Interfaces.Repositories;

public interface IPaymentRefundRepository : IRepository<PaymentRefund>
{
    // Loads a Stripe refund with its payment, ready to be updated.
    Task<PaymentRefund?> GetStripeByProviderRefundIdAsync(string providerRefundId, CancellationToken cancellationToken = default);
}
