using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.HasIndex(rs => new { rs.RouteDirectionId, rs.SequenceOrder }).IsUnique();

        // Route <-> RouteStop (1:N)
        builder.HasOne(rs => rs.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(rs => rs.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stop => stop.RouteDirection)
            .WithMany(direction => direction.Stops)
            .HasForeignKey(stop => stop.RouteDirectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
