using api.Data;
using api.DTO.Routes;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Route = api.Entities.Route;

namespace api.Repositories;

public sealed class RouteRepository(AppDbContext db) : Repository<Route>(db), IRouteRepository
{
    public async Task<List<Route>> SearchAsync(RouteFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(route => route.Centre)
            .Include(route => route.Directions).ThenInclude(direction => direction.StartCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.EndCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.Stops)
            .Include(route => route.Directions).ThenInclude(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay)
            .AsQueryable();

        if (filter.CentreId.HasValue) query = query.Where(route => route.CentreId == filter.CentreId.Value);
        if (filter.Active.HasValue) query = query.Where(route => route.IsActive == filter.Active.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(route => route.RouteNumber.ToLower().Contains(term)
                || route.Name.ToLower().Contains(term)
                || route.Origin.ToLower().Contains(term)
                || route.Destination.ToLower().Contains(term));
        }
        return await query.OrderBy(route => route.RouteNumber).ToListAsync(cancellationToken);
    }

    public Task<Route?> GetWithDetailsAsync(Guid id, bool track = false, CancellationToken cancellationToken = default) =>
        (track ? Set : Set.AsNoTracking())
            .Include(route => route.Centre)
            .Include(route => route.Stops)
            .Include(route => route.Schedules).ThenInclude(schedule => schedule.Bay)
            .Include(route => route.Directions).ThenInclude(direction => direction.StartCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.EndCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.Stops)
            .Include(route => route.Directions).ThenInclude(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay)
            .SingleOrDefaultAsync(route => route.Id == id, cancellationToken);

    public Task<Route?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(route => route.Id == id, cancellationToken);

    public Task<Guid> GetCentreIdAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Set.Where(route => route.Id == routeId).Select(route => route.CentreId).SingleAsync(cancellationToken);

    public Task<bool> RouteNumberExistsAsync(Guid centreId, string routeNumber, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(route => route.CentreId == centreId && route.RouteNumber == routeNumber, cancellationToken);

    public Task<Guid?> GetActiveRouteCentreIdAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Set.Where(route => route.Id == routeId && route.IsActive)
            .Select(route => (Guid?)route.CentreId)
            .SingleOrDefaultAsync(cancellationToken);
}
