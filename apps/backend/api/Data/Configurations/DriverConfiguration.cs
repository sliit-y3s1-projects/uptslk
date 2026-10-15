using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasIndex(d => d.LicenseNumber).IsUnique();
        builder.HasIndex(d => d.UserId).IsUnique();
        builder.Property(d => d.FullName).HasMaxLength(160);

        builder.HasOne(d => d.Centre)
            .WithMany(c => c.Drivers)
            .HasForeignKey(d => d.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        // User <-> Driver (1:1)
        builder.HasOne(d => d.User)
            .WithOne(u => u.Driver)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
