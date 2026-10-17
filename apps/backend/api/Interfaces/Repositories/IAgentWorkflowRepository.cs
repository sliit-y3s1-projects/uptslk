using api.DTO.AgentRecovery;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IAgentWorkflowRepository : IRepository<AgentWorkflow>
{
    Task<List<AgentWorkflow>> SearchAsync(RecoveryWorkflowFilter filter, CancellationToken cancellationToken = default);

    // Loads the full workflow. When a centre is given, workflows from other centres are not returned.
    Task<AgentWorkflow?> GetWithDetailsAsync(Guid id, Guid? centreId, CancellationToken cancellationToken = default);
    // True when the incident already has a running or paused workflow.
    Task<bool> HasActiveForIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);

    // Loads the workflow for an approval decision, with its approvals, trip and incident.
    Task<AgentWorkflow?> GetForDecisionAsync(Guid id, CancellationToken cancellationToken = default);

    // Loads the workflow for the coordinator tools, with the trip's full assignment, the incident and the approvals.
    Task<AgentWorkflow?> GetForCopilotAsync(Guid id, Guid? centreId, CancellationToken cancellationToken = default);

    // Child rows are added to their own sets, because EF treats a new row with a Guid key in a collection as existing.
    void AddStep(AgentStep step);
    void AddApprovalRequest(ApprovalRequest approvalRequest);

    Task<bool> ExistsInCentreAsync(Guid id, Guid centreId, CancellationToken cancellationToken = default);
}
