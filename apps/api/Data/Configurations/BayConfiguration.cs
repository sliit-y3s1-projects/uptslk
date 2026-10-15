using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class BayConfiguration : IEntityTypeConfiguration<Bay>
{
    public void Configure(EntityTypeBuilder<Bay> builder)
    {
        builder.HasIndex(b => new { b.CentreId, b.Code }).IsUnique();
        builder.Property(b => b.Code).HasMaxLength(24);

        builder.HasOne(b => b.Centre)
            .WithMany(c => c.Bays)
            .HasForeignKey(b => b.CentreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
