using Route = api.Entities.Route;

namespace api.Interfaces.Repositories;

public interface IRouteRepository : IRepository<Route>
{
    // The centre of an active route, or null when no active route has this id.
    Task<Guid?> GetActiveRouteCentreIdAsync(Guid routeId, CancellationToken cancellationToken = default);
}
