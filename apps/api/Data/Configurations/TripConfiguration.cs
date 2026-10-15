using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasOne(t => t.Centre)
            .WithMany(c => c.Trips)
            .HasForeignKey(t => t.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Bay)
            .WithMany(b => b.Trips)
            .HasForeignKey(t => t.BayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(trip => trip.RouteDirection)
            .WithMany(direction => direction.Trips)
            .HasForeignKey(trip => trip.RouteDirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Route <-> Trip (1:N)
        builder.HasOne(t => t.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Vehicle <-> Trip (1:N)
        builder.HasOne(t => t.Vehicle)
            .WithMany(v => v.Trips)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Driver <-> Trip (1:N)
        builder.HasOne(t => t.Driver)
            .WithMany(d => d.Trips)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
