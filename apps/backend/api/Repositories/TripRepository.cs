using api.Common.Helpers;
using api.Data;
using api.DTO.Trips;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class TripRepository(AppDbContext db) : Repository<Trip>(db), ITripRepository
{
    public async Task<List<Trip>> SearchAsync(TripFilter filter, CancellationToken cancellationToken = default)
    {
        var query = WithListDetails();
        if (filter.CentreId.HasValue) query = query.Where(trip => trip.CentreId == filter.CentreId.Value);
        if (filter.TerminalId.HasValue)
            query = query.Where(trip => trip.RouteDirection != null
                && (trip.RouteDirection.StartCentreId == filter.TerminalId.Value || trip.RouteDirection.EndCentreId == filter.TerminalId.Value));
        if (filter.RouteId.HasValue) query = query.Where(trip => trip.RouteId == filter.RouteId.Value);
        if (filter.DirectionId.HasValue) query = query.Where(trip => trip.RouteDirectionId == filter.DirectionId.Value);
        if (filter.VehicleId.HasValue) query = query.Where(trip => trip.VehicleId == filter.VehicleId.Value);
        if (filter.DriverId.HasValue) query = query.Where(trip => trip.DriverId == filter.DriverId.Value);
        if (filter.BayId.HasValue) query = query.Where(trip => trip.BayId == filter.BayId.Value);
        if (filter.Status.HasValue) query = query.Where(trip => trip.Status == filter.Status.Value);
        if (filter.BookableOnly)
        {
            var now = DateTime.UtcNow;
            query = query.Where(trip => trip.ScheduledTime > now
                && (trip.Status == TripStatus.Scheduled || trip.Status == TripStatus.Ready || trip.Status == TripStatus.Boarding));
        }
        if (filter.Date.HasValue) query = OnServiceDate(query, filter.Date.Value);
        return await query.OrderBy(trip => trip.ScheduledTime).ToListAsync(cancellationToken);
    }

    public async Task<List<Trip>> GetHistoryAsync(TripHistoryFilter filter, CancellationToken cancellationToken = default)
    {
        var query = WithListDetails().Where(trip => trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled);
        if (filter.CentreId.HasValue) query = query.Where(trip => trip.CentreId == filter.CentreId.Value);
        if (filter.Date.HasValue) query = OnServiceDate(query, filter.Date.Value);
        return await query.OrderByDescending(trip => trip.ScheduledTime).ToListAsync(cancellationToken);
    }

    public Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithListDetails().Include(trip => trip.Centre).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<Trip?> GetWithRouteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(trip => trip.Route).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<Trip?> GetWithRouteAndBayAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(trip => trip.Route).Include(trip => trip.Bay).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);

    public Task<Trip?> GetWithVehicleAndRouteAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? Set : Set.AsNoTracking();
        return query.Include(trip => trip.Vehicle).Include(trip => trip.Route).SingleOrDefaultAsync(trip => trip.Id == id, cancellationToken);
    }

    public Task<List<Trip>> GetActiveForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        Active(Set.AsNoTracking())
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection)
            .Where(trip => trip.VehicleId == vehicleId)
            .ToListAsync(cancellationToken);

    public Task<List<Trip>> GetActiveUsingAnyAsync(Guid vehicleId, Guid driverId, Guid bayId, Guid? excludedTripId, CancellationToken cancellationToken = default) =>
        Active(Set.AsNoTracking())
            .Include(trip => trip.Route)
            .Where(trip => (!excludedTripId.HasValue || trip.Id != excludedTripId.Value)
                && (trip.VehicleId == vehicleId || trip.DriverId == driverId || trip.BayId == bayId))
            .ToListAsync(cancellationToken);

    public Task<List<Trip>> ListActiveInWindowAsync(Guid centreId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        Active(WithListDetails())
            .Where(trip => trip.ScheduledTime >= fromUtc && trip.ScheduledTime < toUtc
                && (trip.CentreId == centreId
                    || (trip.RouteDirection != null && (trip.RouteDirection.StartCentreId == centreId || trip.RouteDirection.EndCentreId == centreId))))
            .OrderBy(trip => trip.ScheduledTime)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAtAsync(Guid routeId, Guid? directionId, DateTime scheduledTime, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(trip => trip.RouteId == routeId && trip.RouteDirectionId == directionId && trip.ScheduledTime == scheduledTime, cancellationToken);

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

    private IQueryable<Trip> WithListDetails() =>
        Set.AsNoTracking()
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(trip => trip.Vehicle)
            .Include(trip => trip.Driver)
            .Include(trip => trip.Bay);

    private static IQueryable<Trip> OnServiceDate(IQueryable<Trip> query, DateOnly date)
    {
        var start = SriLankaTime.DayStartUtc(date);
        var end = SriLankaTime.DayStartUtc(date.AddDays(1));
        return query.Where(trip => trip.ScheduledTime >= start && trip.ScheduledTime < end);
    }

    // Trips that still hold a vehicle, driver and bay.
    private static IQueryable<Trip> Active(IQueryable<Trip> query) =>
        query.Where(trip => trip.Status != TripStatus.Completed && trip.Status != TripStatus.Cancelled);
}
