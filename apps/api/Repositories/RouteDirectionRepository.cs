using api.Data;
using api.Interfaces.Repositories;
using RouteDirection = api.Entities.RouteDirection;

namespace api.Repositories;

public sealed class RouteDirectionRepository(AppDbContext db) : Repository<RouteDirection>(db), IRouteDirectionRepository
{
}
