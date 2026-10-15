using api.Entities;

namespace api.Interfaces.Repositories;

public interface IBayRepository : IRepository<Bay>
{
    Task<List<Bay>> ListForCentreAsync(Guid centreId, CancellationToken cancellationToken = default);
    Task<Bay?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    // True when another bay at the centre already uses the code.
    Task<bool> CodeExistsAsync(Guid centreId, string code, Guid? exceptBayId = null, CancellationToken cancellationToken = default);
}
