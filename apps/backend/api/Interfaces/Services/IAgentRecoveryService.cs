using api.DTO.AgentRecovery;

namespace api.Interfaces.Services;

// The AI-assisted incident recovery workflows as seen by centre staff.
public interface IAgentRecoveryService
{
    Task<IReadOnlyList<RecoveryWorkflowListItemResponse>> ListAsync(RecoveryWorkflowFilter filter, CancellationToken cancellationToken);
    Task<RecoveryWorkflowResponse> GetAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<RecoveryWorkflowStatusResponse> StartAsync(StartRecoveryWorkflowRequest request, CancellationToken cancellationToken);
    Task<RecoveryWorkflowStatusResponse> DecideAsync(Guid workflowId, DecideRecoveryApprovalRequest request, CancellationToken cancellationToken);
}
