using Microsoft.EntityFrameworkCore.Storage;

namespace api.Interfaces.Repositories;

// Repositories only track changes. Services call this to save them all together.
public interface IUnitOfWork
{
    // True while a transaction is open, for example when one service runs inside another service's transaction.
    bool HasActiveTransaction { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    // Forgets every change that has not been saved yet and every loaded entity.
    void DiscardChanges();
}
