using RouteDirection = api.Entities.RouteDirection;

namespace api.Interfaces.Repositories;

public interface IRouteDirectionRepository : IRepository<RouteDirection>
{
    Task<List<RouteDirection>> ListForRouteAsync(Guid routeId, CancellationToken cancellationToken = default);

    // Loads both centres, the stops, and the schedules with their bays.
    Task<RouteDirection?> GetWithDetailsAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<RouteDirection?> GetForRouteAsync(Guid directionId, Guid routeId, CancellationToken cancellationToken = default);
}
