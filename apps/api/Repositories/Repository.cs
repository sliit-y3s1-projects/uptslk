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

    public void Add(T entity) => Set.Add(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
