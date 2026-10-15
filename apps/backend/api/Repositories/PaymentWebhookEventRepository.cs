using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PaymentWebhookEventRepository(AppDbContext db) : Repository<PaymentWebhookEvent>(db), IPaymentWebhookEventRepository
{
    public Task<bool> IsProcessedAsync(PaymentProvider provider, string providerEventId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(webhook => webhook.Provider == provider && webhook.ProviderEventId == providerEventId, cancellationToken);
}
