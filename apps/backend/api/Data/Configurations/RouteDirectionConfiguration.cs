using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteDirection = api.Entities.RouteDirection;

namespace api.Data.Configurations;

public sealed class RouteDirectionConfiguration : IEntityTypeConfiguration<RouteDirection>
{
    public void Configure(EntityTypeBuilder<RouteDirection> builder)
    {
        builder.HasIndex(direction => new { direction.RouteId, direction.StartCentreId, direction.EndCentreId }).IsUnique();

        builder.HasOne(direction => direction.Route)
            .WithMany(route => route.Directions)
            .HasForeignKey(direction => direction.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(direction => direction.StartCentre)
            .WithMany()
            .HasForeignKey(direction => direction.StartCentreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(direction => direction.EndCentre)
            .WithMany()
            .HasForeignKey(direction => direction.EndCentreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
