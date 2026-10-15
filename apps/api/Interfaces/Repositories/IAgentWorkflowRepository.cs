using api.DTO.AgentRecovery;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IAgentWorkflowRepository : IRepository<AgentWorkflow>
{
    Task<List<AgentWorkflow>> SearchAsync(RecoveryWorkflowFilter filter, CancellationToken cancellationToken = default);

    // Loads the full workflow. When a centre is given, workflows from other centres are not returned.
    Task<AgentWorkflow?> GetWithDetailsAsync(Guid id, Guid? centreId, CancellationToken cancellationToken = default);
    Task<bool> ExistsInCentreAsync(Guid id, Guid centreId, CancellationToken cancellationToken = default);
}
