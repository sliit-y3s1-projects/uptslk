using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<List<User>> ListAsync(bool staffOnly, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking();
        if (staffOnly) query = query.Where(user => user.Role != UserRole.Commuter && user.Role != UserRole.Admin);
        return await query.OrderBy(user => user.Name).ToListAsync(cancellationToken);
    }
}
