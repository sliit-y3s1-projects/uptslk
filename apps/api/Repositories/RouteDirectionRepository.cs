using api.Data;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using RouteDirection = api.Entities.RouteDirection;

namespace api.Repositories;

public sealed class RouteDirectionRepository(AppDbContext db) : Repository<RouteDirection>(db), IRouteDirectionRepository
{
    public Task<List<RouteDirection>> ListForRouteAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        WithDetails(Set.AsNoTracking())
            .Where(direction => direction.RouteId == routeId)
            .OrderBy(direction => direction.Name)
            .ToListAsync(cancellationToken);

    public Task<RouteDirection?> GetWithDetailsAsync(Guid id, bool track = false, CancellationToken cancellationToken = default) =>
        WithDetails(track ? Set : Set.AsNoTracking()).SingleOrDefaultAsync(direction => direction.Id == id, cancellationToken);

    public Task<RouteDirection?> GetForRouteAsync(Guid directionId, Guid routeId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(direction => direction.Id == directionId && direction.RouteId == routeId, cancellationToken);

    private static IQueryable<RouteDirection> WithDetails(IQueryable<RouteDirection> query) =>
        query.Include(direction => direction.StartCentre)
            .Include(direction => direction.EndCentre)
            .Include(direction => direction.Stops)
            .Include(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay);
}
