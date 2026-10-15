using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class TripRepository(AppDbContext db) : Repository<Trip>(db), ITripRepository
{
    public Task<Trip?> GetWithRouteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(trip => trip.Route).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);
}
