using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class TripRepository(AppDbContext db) : Repository<Trip>(db), ITripRepository
{
    public Task<Trip?> GetWithRouteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(trip => trip.Route).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<Trip?> GetWithVehicleAndRouteAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? Set : Set.AsNoTracking();
        return query.Include(trip => trip.Vehicle).Include(trip => trip.Route).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);
    }

    public Task<bool> ExistsInCentreAsync(Guid tripId, Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(trip => trip.Id == tripId && trip.CentreId == centreId, cancellationToken);

    public Task<Trip?> GetForDriverAsync(Guid tripId, Guid driverId, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? Set : Set.AsNoTracking();
        return query.SingleOrDefaultAsync(trip => trip.Id == tripId && trip.DriverId == driverId, cancellationToken);
    }

    public Task<List<Trip>> GetDriverDutiesAsync(Guid driverId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(trip => trip.Vehicle)
            .Include(trip => trip.Bay)
            .Include(trip => trip.Bookings)
            .Where(trip => trip.DriverId == driverId && trip.ScheduledTime >= startUtc && trip.ScheduledTime < endUtc)
            .OrderBy(trip => trip.ScheduledTime)
            .ToListAsync(cancellationToken);
}
