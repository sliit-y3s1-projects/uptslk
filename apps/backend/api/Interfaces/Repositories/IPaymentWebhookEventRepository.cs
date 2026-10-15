using api.Entities;
using api.Entities.Enums;

namespace api.Interfaces.Repositories;

// Webhook events we have already handled, so a resent event is not applied twice.
public interface IPaymentWebhookEventRepository : IRepository<PaymentWebhookEvent>
{
    Task<bool> IsProcessedAsync(PaymentProvider provider, string providerEventId, CancellationToken cancellationToken = default);
}
