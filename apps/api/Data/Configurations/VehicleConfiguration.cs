using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasIndex(v => v.PlateNumber).IsUnique();
        builder.Property(v => v.Model).HasMaxLength(120);
        builder.Property(v => v.ImageUrl).HasMaxLength(2048);

        builder.HasOne(v => v.Centre)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CentreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
