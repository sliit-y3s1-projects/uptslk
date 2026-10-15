using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class CentreConfiguration : IEntityTypeConfiguration<Centre>
{
    public void Configure(EntityTypeBuilder<Centre> builder)
    {
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Code).HasMaxLength(32);
        builder.Property(c => c.Name).HasMaxLength(160);
    }
}
