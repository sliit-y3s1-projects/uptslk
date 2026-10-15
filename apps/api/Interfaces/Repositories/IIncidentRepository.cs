using api.DTO.Incidents;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IIncidentRepository : IRepository<Incident>
{
    Task<List<Incident>> SearchAsync(IncidentFilter filter, CancellationToken cancellationToken = default);
    Task<Incident?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsInCentreAsync(Guid id, Guid centreId, CancellationToken cancellationToken = default);
}
