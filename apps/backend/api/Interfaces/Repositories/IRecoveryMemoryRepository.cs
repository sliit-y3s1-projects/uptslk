using api.Entities;
using api.Entities.Enums;

namespace api.Interfaces.Repositories;

public interface IRecoveryMemoryRepository : IRepository<RecoveryMemory>
{
    // The newest decisions at the centre for the same route and incident type, rejections and approvals both.
    Task<List<RecoveryMemory>> ListForContextAsync(Guid centreId, string routeNumber, IncidentType incidentType, int take, CancellationToken cancellationToken = default);
}
