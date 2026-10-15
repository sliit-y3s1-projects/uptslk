using api.Entities;

namespace api.Interfaces.Repositories;

public interface IRouteStopRepository : IRepository<RouteStop>
{
    void AddRange(IEnumerable<RouteStop> stops);
    void RemoveRange(IEnumerable<RouteStop> stops);
}
