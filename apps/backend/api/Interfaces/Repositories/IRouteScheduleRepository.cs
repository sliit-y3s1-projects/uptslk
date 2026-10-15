using api.Entities;

namespace api.Interfaces.Repositories;

public interface IRouteScheduleRepository : IRepository<RouteSchedule>
{
    Task<List<RouteSchedule>> ListForRouteAsync(Guid routeId, CancellationToken cancellationToken = default);

    // Loads the schedule for editing with its route, direction and bay.
    Task<RouteSchedule?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
