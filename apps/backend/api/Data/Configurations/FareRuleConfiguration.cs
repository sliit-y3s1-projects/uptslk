using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class FareRuleConfiguration : IEntityTypeConfiguration<FareRule>
{
    public void Configure(EntityTypeBuilder<FareRule> builder)
    {
        builder.HasIndex(rule => rule.RouteId).IsUnique();
        builder.Property(rule => rule.Amount).HasPrecision(12, 2);

        builder.HasOne(rule => rule.Route)
            .WithMany(route => route.FareRules)
            .HasForeignKey(rule => rule.RouteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
