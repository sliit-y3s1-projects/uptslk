using api.Enums;

namespace api.Models;

public class ApprovalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = default!;

    public string Reason { get; set; } = default!;

    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }

    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public DateTime? DecidedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}