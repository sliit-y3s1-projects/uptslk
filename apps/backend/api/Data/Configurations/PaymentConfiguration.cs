using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasIndex(payment => new { payment.Provider, payment.ProviderOrderId }).IsUnique();
        builder.Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(3);
        builder.Property(payment => payment.ProviderOrderId).HasMaxLength(96);
        builder.Property(payment => payment.ProviderCheckoutId).HasMaxLength(128);
        builder.Property(payment => payment.ProviderPaymentId).HasMaxLength(96);

        builder.HasOne(payment => payment.Booking)
            .WithMany(booking => booking.Payments)
            .HasForeignKey(payment => payment.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
