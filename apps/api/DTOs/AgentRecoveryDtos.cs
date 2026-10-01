using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class StartRecoveryWorkflowRequest
{
    public Guid IncidentId { get; init; }
    [StringLength(1000)] public string? Objective { get; init; }
    /// <summary>
    /// When false, the workflow is not started if the AI planner is unavailable, so the
    /// caller can ask the user whether to continue with the safe built-in plan.
    /// </summary>
    public bool AllowFallback { get; init; } = true;
}

public sealed class DecideRecoveryApprovalRequest
{
    public ApprovalDecision Decision { get; init; }
    [StringLength(2000)] public string? Note { get; init; }
}
