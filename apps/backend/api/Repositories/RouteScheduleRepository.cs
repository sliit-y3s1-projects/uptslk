using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class RouteScheduleRepository(AppDbContext db) : Repository<RouteSchedule>(db), IRouteScheduleRepository
{
    public Task<List<RouteSchedule>> ListForRouteAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(schedule => schedule.Bay)
            .Where(schedule => schedule.RouteId == routeId)
            .OrderBy(schedule => schedule.FirstDeparture)
            .ToListAsync(cancellationToken);

    public Task<RouteSchedule?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(schedule => schedule.Route)
            .Include(schedule => schedule.RouteDirection)
            .Include(schedule => schedule.Bay)
            .SingleOrDefaultAsync(schedule => schedule.Id == id, cancellationToken);
}
