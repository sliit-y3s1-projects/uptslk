using System.ComponentModel.DataAnnotations;
using api.Entities.Enums;

namespace api.DTO.AgentRecovery;

public sealed class StartRecoveryWorkflowRequest
{
    public Guid IncidentId { get; init; }
    [StringLength(1000)] public string? Objective { get; init; }
    // When false and the AI planner is down, the workflow does not start so the user can choose the safe built-in plan.
    public bool AllowFallback { get; init; } = true;
}

public sealed class DecideRecoveryApprovalRequest
{
    public ApprovalDecision Decision { get; init; }
    [StringLength(2000)] public string? Note { get; init; }
}
