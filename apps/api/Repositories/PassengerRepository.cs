using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PassengerRepository(AppDbContext db) : Repository<Passenger>(db), IPassengerRepository
{
    public Task<Passenger?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(passenger => passenger.Id == id && passenger.IsActive, cancellationToken);

    public Task<Guid?> GetActiveIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(passenger => passenger.UserId == userId && passenger.IsActive)
            .Select(passenger => (Guid?)passenger.Id)
            .SingleOrDefaultAsync(cancellationToken);
}
