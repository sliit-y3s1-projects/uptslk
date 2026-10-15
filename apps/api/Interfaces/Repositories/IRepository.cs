namespace api.Interfaces.Repositories;

// The basic operations every repository has. Feature repositories add their own named queries.
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Remove(T entity);
}
