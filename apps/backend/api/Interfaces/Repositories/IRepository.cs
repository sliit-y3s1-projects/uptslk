namespace api.Interfaces.Repositories;

// The basic operations every repository has. Feature repositories add their own named queries.
public interface IRepository<T> where T : class
{
    // Loads the entity for editing. Changes to it are saved by IUnitOfWork.
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Loads the entity without tracking it. Use this when the entity is only read.
    Task<T?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Remove(T entity);

    // Locks the row until the transaction ends, so parallel requests on it run one at a time. Use inside a transaction.
    Task LockAsync(Guid id, CancellationToken cancellationToken = default);

    // Locks the entity's row like LockAsync, then reloads its values so later checks see the latest data.
    Task LockAndReloadAsync(T entity, CancellationToken cancellationToken = default);
}
