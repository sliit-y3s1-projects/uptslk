using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.Property(request => request.Reason).HasMaxLength(2000);
        builder.Property(request => request.DecisionNote).HasMaxLength(2000);

        // AgentWorkflow <-> ApprovalRequest (1:N)
        builder.HasOne(ar => ar.Workflow)
            .WithMany(w => w.ApprovalRequests)
            .HasForeignKey(ar => ar.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // User <-> ApprovalRequest (1:N, as reviewer, nullable)
        builder.HasOne(ar => ar.ReviewedBy)
            .WithMany(u => u.ReviewedApprovals)
            .HasForeignKey(ar => ar.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
