using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace api.Data;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        db.Database.BeginTransactionAsync(cancellationToken);

    public void DiscardChanges() => db.ChangeTracker.Clear();
}
