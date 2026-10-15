using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Route = api.Entities.Route;

namespace api.Data.Configurations;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasIndex(r => new { r.CentreId, r.RouteNumber }).IsUnique();
        builder.Property(r => r.RouteNumber).HasMaxLength(32);

        builder.HasOne(r => r.Centre)
            .WithMany(c => c.Routes)
            .HasForeignKey(r => r.CentreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
