using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class AgentWorkflowConfiguration : IEntityTypeConfiguration<AgentWorkflow>
{
    public void Configure(EntityTypeBuilder<AgentWorkflow> builder)
    {
        builder.Property(workflow => workflow.Objective).HasMaxLength(1000);
        builder.Property(workflow => workflow.FailureReason).HasMaxLength(2000);
        builder.Property(workflow => workflow.PlanningMode).HasMaxLength(32);
        builder.Property(workflow => workflow.ModelProvider).HasMaxLength(64);
        builder.Property(workflow => workflow.ModelName).HasMaxLength(120);
        builder.Property(workflow => workflow.PromptVersion).HasMaxLength(80);
        builder.Property(workflow => workflow.PlanningFallbackReason).HasMaxLength(2000);
        builder.Property(workflow => workflow.PlanJson).HasColumnType("jsonb");
        builder.Property(workflow => workflow.PlannerInputJson).HasColumnType("jsonb");
        builder.Property(workflow => workflow.PlannerOutputJson).HasColumnType("jsonb");
        builder.Property(workflow => workflow.ReplanHistoryJson).HasColumnType("jsonb");
        builder.Property(workflow => workflow.ValidationJson).HasColumnType("jsonb");
        builder.Property(workflow => workflow.NotificationDraftsJson).HasColumnType("jsonb");
        builder.Property(workflow => workflow.ImpactReviewsJson).HasColumnType("jsonb");

        // Booking <-> AgentWorkflow (1:1, nullable)
        builder.HasOne(aw => aw.Booking)
            .WithOne(b => b.AgentWorkflow)
            .HasForeignKey<AgentWorkflow>(aw => aw.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(workflow => workflow.Centre)
            .WithMany()
            .HasForeignKey(workflow => workflow.CentreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workflow => workflow.Incident)
            .WithMany()
            .HasForeignKey(workflow => workflow.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workflow => workflow.Trip)
            .WithMany()
            .HasForeignKey(workflow => workflow.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(workflow => new { workflow.CentreId, workflow.Status });
        builder.HasIndex(workflow => workflow.IncidentId);
    }
}
