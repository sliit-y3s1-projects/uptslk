using api.Data;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public abstract class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    protected AppDbContext Db { get; } = db;
    protected DbSet<T> Set => Db.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    // Every entity uses a Guid primary key named "Id".
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(entity => EF.Property<Guid>(entity, "Id") == id, cancellationToken);

    public void Add(T entity) => Set.Add(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public async Task LockAndReloadAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entry = Db.Entry(entity);
        var table = entry.Metadata.GetTableName()!;
        var id = (Guid)entry.Property("Id").CurrentValue!;
        await Db.LockRowAsync(table, id, cancellationToken);
        await entry.ReloadAsync(cancellationToken);
    }
}
