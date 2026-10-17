using api.DTO.AgentRecovery;

namespace api.Interfaces.Services;

// The coordinator's tools for steering a recovery proposal while it waits for approval.
public interface IRecoveryCopilotService
{
    // Fleet: try other buses, drivers and delays and compare their scores.
    Task<RecoveryAlternativesResponse> GetAlternativesAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<RecoveryAlternativeResponse> SimulateAsync(Guid workflowId, SimulateScenarioRequest request, CancellationToken cancellationToken);
    Task<RecoveryWorkflowStatusResponse> ApplyAlternativeAsync(Guid workflowId, SimulateScenarioRequest request, CancellationToken cancellationToken);

    // Network: which other trips the recovery disturbs.
    Task<RecoveryImpactResponse> GetImpactAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<ImpactExplanationResponse> ExplainImpactAsync(Guid workflowId, ExplainImpactRequest request, CancellationToken cancellationToken);
    Task<RecoveryImpactResponse> ReviewImpactAsync(Guid workflowId, ReviewImpactRequest request, CancellationToken cancellationToken);

    // Passenger: preview, edit and rewrite the messages each group will get.
    Task<RecoveryMessagesResponse> GetMessagesAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<MessageSegmentResponse> SaveMessageAsync(Guid workflowId, string segment, SaveMessageDraftRequest request, CancellationToken cancellationToken);
    Task<MessageSegmentResponse> RegenerateMessageAsync(Guid workflowId, string segment, RegenerateMessageRequest request, CancellationToken cancellationToken);
    Task<MessageSegmentResponse> ResetMessageAsync(Guid workflowId, string segment, CancellationToken cancellationToken);

    // Dispatch: reject with a reason, let the agent revise, and see what earlier decisions taught it.
    Task<ReviseRecoveryResponse> ReviseAsync(Guid workflowId, ReviseRecoveryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryLessonResponse>> GetLessonsAsync(Guid workflowId, CancellationToken cancellationToken);
}
