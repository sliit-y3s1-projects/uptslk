using api.Data;
using api.Entities;
using api.Interfaces.Repositories;

namespace api.Repositories;

public sealed class RouteStopRepository(AppDbContext db) : Repository<RouteStop>(db), IRouteStopRepository
{
    public void AddRange(IEnumerable<RouteStop> stops) => Set.AddRange(stops);

    public void RemoveRange(IEnumerable<RouteStop> stops) => Set.RemoveRange(stops);
}
