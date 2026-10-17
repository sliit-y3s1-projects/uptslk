using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class RecoveryMemoryConfiguration : IEntityTypeConfiguration<RecoveryMemory>
{
    public void Configure(EntityTypeBuilder<RecoveryMemory> builder)
    {
        builder.Property(memory => memory.RouteNumber).HasMaxLength(32);
        builder.Property(memory => memory.Reason).HasMaxLength(2000);
        builder.Property(memory => memory.Summary).HasMaxLength(500);

        builder.HasOne(memory => memory.Centre)
            .WithMany()
            .HasForeignKey(memory => memory.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(memory => memory.Workflow)
            .WithMany()
            .HasForeignKey(memory => memory.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(memory => new { memory.CentreId, memory.RouteNumber, memory.IncidentType });
        builder.HasIndex(memory => memory.WorkflowId);
    }
}
