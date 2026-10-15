using api.Entities;

namespace api.Interfaces.Repositories;

public interface ITripRepository : IRepository<Trip>
{
    Task<Trip?> GetWithRouteAsync(Guid id, CancellationToken cancellationToken = default);
}
