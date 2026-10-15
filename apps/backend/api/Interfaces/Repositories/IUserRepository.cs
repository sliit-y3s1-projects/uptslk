using api.Entities;

namespace api.Interfaces.Repositories;

// Read-only user queries. Changes to users go through ASP.NET Identity's UserManager.
public interface IUserRepository
{
    // All users by name. Staff only leaves out commuters and admins.
    Task<List<User>> ListAsync(bool staffOnly, CancellationToken cancellationToken = default);
}
