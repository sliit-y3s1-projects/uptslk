using api.Data;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Route = api.Entities.Route;

namespace api.Repositories;

public sealed class RouteRepository(AppDbContext db) : Repository<Route>(db), IRouteRepository
{
    public Task<Guid?> GetActiveRouteCentreIdAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Set.Where(route => route.Id == routeId && route.IsActive)
            .Select(route => (Guid?)route.CentreId)
            .SingleOrDefaultAsync(cancellationToken);
}
