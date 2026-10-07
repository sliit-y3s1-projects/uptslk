using Microsoft.EntityFrameworkCore;

namespace api.Data;

public static class DbContextLockExtensions
{
    /// <summary>
    /// Takes a PostgreSQL row lock (SELECT ... FOR UPDATE) that is held until the current transaction ends, so concurrent
    /// requests that touch the same row are processed one after another. Call it inside a transaction. Other providers
    /// (the in-memory SQLite used by unit tests) serialize writes themselves, so no lock is taken there.
    /// </summary>
    public static Task LockRowAsync(this AppDbContext db, string table, Guid id, CancellationToken cancellationToken = default) =>
        db.Database.IsNpgsql()
            ? db.Database.ExecuteSqlRawAsync($"SELECT 1 FROM \"{table}\" WHERE \"Id\" = {{0}} FOR UPDATE", [id], cancellationToken)
            : Task.CompletedTask;
}
