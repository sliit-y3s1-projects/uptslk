using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.HasIndex(p => p.PhoneNumber).IsUnique();
        builder.HasOne(p => p.User).WithOne(u => u.Passenger).HasForeignKey<Passenger>(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(p => p.FullName).HasMaxLength(160);
        builder.Property(p => p.PhoneNumber).HasMaxLength(32);
    }
}
