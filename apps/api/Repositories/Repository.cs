using api.Data;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public abstract class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    // Every entity uses a Guid primary key named "Id".
    private const string KeyName = "Id";

    protected AppDbContext Db { get; } = db;
    protected DbSet<T> Set => Db.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public Task<T?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(entity => EF.Property<Guid>(entity, KeyName) == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(entity => EF.Property<Guid>(entity, KeyName) == id, cancellationToken);

    public void Add(T entity) => Set.Add(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task LockAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.LockRowAsync(Db.Model.FindEntityType(typeof(T))!.GetTableName()!, id, cancellationToken);

    public async Task LockAndReloadAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entry = Db.Entry(entity);
        await LockAsync((Guid)entry.Property(KeyName).CurrentValue!, cancellationToken);
        await entry.ReloadAsync(cancellationToken);
    }
}
