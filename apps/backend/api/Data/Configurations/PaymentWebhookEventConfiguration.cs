using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.HasIndex(webhook => new { webhook.Provider, webhook.ProviderEventId }).IsUnique();
        builder.Property(webhook => webhook.ProviderEventId).HasMaxLength(128);
        builder.Property(webhook => webhook.EventType).HasMaxLength(128);
    }
}
