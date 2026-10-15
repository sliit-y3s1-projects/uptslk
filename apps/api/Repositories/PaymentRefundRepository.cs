using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PaymentRefundRepository(AppDbContext db) : Repository<PaymentRefund>(db), IPaymentRefundRepository
{
    public Task<PaymentRefund?> GetStripeByProviderRefundIdAsync(string providerRefundId, CancellationToken cancellationToken = default) =>
        Set.Include(refund => refund.Payment)
            .SingleOrDefaultAsync(refund => refund.ProviderRefundId == providerRefundId && refund.Payment.Provider == PaymentProvider.Stripe, cancellationToken);
}
