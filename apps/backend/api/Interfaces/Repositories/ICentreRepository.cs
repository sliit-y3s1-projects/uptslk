using api.DTO.Centres;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface ICentreRepository : IRepository<Centre>
{
    // Loads each centre with its bays and only its active routes.
    Task<List<Centre>> SearchAsync(CentreFilter filter, CancellationToken cancellationToken = default);
    Task<Centre?> GetWithBaysAndRoutesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<int> CountExistingAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
