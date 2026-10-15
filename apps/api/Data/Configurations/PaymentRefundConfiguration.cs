using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        builder.Property(refund => refund.Amount).HasPrecision(12, 2);
        builder.Property(refund => refund.Reason).HasMaxLength(1000);

        builder.HasOne(refund => refund.Payment)
            .WithMany(payment => payment.Refunds)
            .HasForeignKey(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
