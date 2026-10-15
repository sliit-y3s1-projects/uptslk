using api.DTO.Routes;
using Route = api.Entities.Route;

namespace api.Interfaces.Repositories;

public interface IRouteRepository : IRepository<Route>
{
    Task<List<Route>> SearchAsync(RouteFilter filter, CancellationToken cancellationToken = default);

    // Loads the centre, stops, schedules and every direction with its centres, stops and schedules.
    Task<Route?> GetWithDetailsAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<Route?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> GetCentreIdAsync(Guid routeId, CancellationToken cancellationToken = default);
    Task<bool> RouteNumberExistsAsync(Guid centreId, string routeNumber, CancellationToken cancellationToken = default);

    // The centre of an active route, or null when no active route has this id.
    Task<Guid?> GetActiveRouteCentreIdAsync(Guid routeId, CancellationToken cancellationToken = default);
}
