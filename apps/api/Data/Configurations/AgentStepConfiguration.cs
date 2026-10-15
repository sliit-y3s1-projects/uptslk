using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class AgentStepConfiguration : IEntityTypeConfiguration<AgentStep>
{
    public void Configure(EntityTypeBuilder<AgentStep> builder)
    {
        builder.Property(step => step.AgentName).HasMaxLength(120);
        builder.Property(step => step.Status).HasMaxLength(32);
        builder.Property(step => step.Error).HasMaxLength(2000);
        builder.Property(step => step.ToolCallsJson).HasColumnType("jsonb");

        // AgentWorkflow <-> AgentStep (1:N)
        builder.HasOne(s => s.Workflow)
            .WithMany(w => w.Steps)
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
