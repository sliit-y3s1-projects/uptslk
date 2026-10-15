using api.Entities.Enums;

namespace api.DTO.AgentRecovery;

public sealed class RecoveryWorkflowFilter
{
    public Guid? CentreId { get; init; }
    public WorkflowStatus? Status { get; init; }
}
