using Microsoft.EntityFrameworkCore;

namespace api.Data;

public static class DbContextLockExtensions
{
    // Locks a row (SELECT ... FOR UPDATE) until the transaction ends. SQLite in tests needs no lock, so none is taken there.
    public static Task LockRowAsync(this AppDbContext db, string table, Guid id, CancellationToken cancellationToken = default) =>
        db.Database.IsNpgsql()
            ? db.Database.ExecuteSqlRawAsync($"SELECT 1 FROM \"{table}\" WHERE \"Id\" = {{0}} FOR UPDATE", [id], cancellationToken)
            : Task.CompletedTask;
}
