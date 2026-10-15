using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.Property(wallet => wallet.Balance).HasPrecision(12, 2);

        builder.HasOne(w => w.Passenger)
            .WithOne(p => p.Wallet)
            .HasForeignKey<Wallet>(w => w.PassengerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
