using api.DTO.Fares;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IFareRuleRepository : IRepository<FareRule>
{
    Task<List<FareRule>> SearchAsync(FareRuleFilter filter, CancellationToken cancellationToken = default);
    Task<FareRule?> GetWithRouteAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<FareRule?> GetActiveForRouteAsync(Guid routeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForRouteAsync(Guid routeId, CancellationToken cancellationToken = default);
}
