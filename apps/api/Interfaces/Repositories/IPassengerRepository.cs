using api.Entities;

namespace api.Interfaces.Repositories;

public interface IPassengerRepository : IRepository<Passenger>
{
    Task<Passenger?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetActiveIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
