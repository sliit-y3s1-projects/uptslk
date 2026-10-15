namespace api.Interfaces.Repositories;

// The basic operations every repository has. Feature repositories add their own named queries.
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Remove(T entity);

    // Locks the entity's row until the current transaction ends, then reloads its values.
    // Parallel requests that touch the same row then run one after another.
    Task LockAndReloadAsync(T entity, CancellationToken cancellationToken = default);
}
