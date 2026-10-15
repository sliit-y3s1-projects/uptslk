using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.Property(m => m.Type).HasMaxLength(80);

        builder.HasOne(record => record.Vehicle)
            .WithMany(vehicle => vehicle.MaintenanceRecords)
            .HasForeignKey(record => record.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
