using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class StartRecoveryWorkflowRequest
{
    public Guid IncidentId { get; init; }
    [StringLength(1000)] public string? Objective { get; init; }
}

public sealed class DecideRecoveryApprovalRequest
{
    public ApprovalDecision Decision { get; init; }
    [StringLength(2000)] public string? Note { get; init; }
}
