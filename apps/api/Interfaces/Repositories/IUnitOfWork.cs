using Microsoft.EntityFrameworkCore.Storage;

namespace api.Interfaces.Repositories;

// Repositories only track changes. Services call this to save them all together.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
