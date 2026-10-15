using api.Data;
using api.DTO.Fares;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class FareRuleRepository(AppDbContext db) : Repository<FareRule>(db), IFareRuleRepository
{
    public async Task<List<FareRule>> SearchAsync(FareRuleFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(rule => rule.Route).AsQueryable();
        if (filter.RouteId.HasValue) query = query.Where(rule => rule.RouteId == filter.RouteId.Value);
        if (filter.CentreId.HasValue) query = query.Where(rule => rule.Route.CentreId == filter.CentreId.Value);
        if (filter.Active.HasValue) query = query.Where(rule => rule.IsActive == filter.Active.Value);
        return await query.OrderBy(rule => rule.Route.RouteNumber).ToListAsync(cancellationToken);
    }

    public Task<FareRule?> GetWithRouteAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? Set : Set.AsNoTracking();
        return query.Include(rule => rule.Route).SingleOrDefaultAsync(rule => rule.Id == id, cancellationToken);
    }

    public Task<FareRule?> GetActiveForRouteAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(rule => rule.RouteId == routeId && rule.IsActive, cancellationToken);

    public Task<bool> ExistsForRouteAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(rule => rule.RouteId == routeId, cancellationToken);
}
