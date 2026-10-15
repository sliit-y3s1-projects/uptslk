using api.Data;
using api.DTO.Bookings;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class BookingRepository(AppDbContext db) : Repository<Booking>(db), IBookingRepository
{
    public Task<List<Booking>> GetTicketsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Bay)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(booking => booking.Passenger)
            .Where(booking => booking.Passenger.UserId == userId)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<Booking>> SearchAsync(BookingFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(booking => booking.Passenger)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .AsQueryable();
        if (filter.PassengerId.HasValue) query = query.Where(booking => booking.PassengerId == filter.PassengerId.Value);
        if (filter.TripId.HasValue) query = query.Where(booking => booking.TripId == filter.TripId.Value);
        if (filter.Status.HasValue) query = query.Where(booking => booking.Status == filter.Status.Value);
        return await query.OrderByDescending(booking => booking.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(booking => booking.Passenger).ThenInclude(passenger => passenger.Wallet)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.RouteDirection)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Vehicle)
            .Include(booking => booking.Transactions)
            .SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public Task<Booking?> GetWithPassengerAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(booking => booking.Passenger).SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public Task<List<Booking>> GetActiveForTripWithPassengerAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(booking => booking.Passenger)
            .Where(booking => booking.TripId == tripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .ToListAsync(cancellationToken);

    public Task<Booking?> GetForCancellationAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(booking => booking.Payments)
            .Include(booking => booking.Passenger).ThenInclude(passenger => passenger.Wallet)
            .SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public Task<Booking?> GetWithPaymentsAndRefundsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(booking => booking.Payments).ThenInclude(payment => payment.Refunds)
            .SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public Task<List<Guid>> GetActiveIdsForTripAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Set.Where(booking => booking.TripId == tripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken);

    public Task<List<Booking>> GetActiveWithoutRecoveryNoticeAsync(Guid tripId, Guid workflowId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(booking => booking.Passenger)
            .Where(booking => booking.TripId == tripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
                && !Db.PassengerNotifications.Any(notification =>
                    notification.WorkflowId == workflowId
                    && notification.BookingId == booking.Id
                    && notification.Channel == PassengerNotificationChannel.InApp))
            .OrderBy(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Booking?> GetWithTripAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(booking => booking.Trip).SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public async Task<int> GetOccupiedCapacityAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        await Set.Where(booking => booking.TripId == tripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .SumAsync(booking => (int?)booking.PassengerCount, cancellationToken) ?? 0;

    public Task<Dictionary<Guid, int>> GetOccupiedCapacityByTripAsync(IReadOnlyCollection<Guid> tripIds, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(booking => tripIds.Contains(booking.TripId)
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .GroupBy(booking => booking.TripId)
            .Select(group => new { TripId = group.Key, Count = group.Sum(booking => booking.PassengerCount) })
            .ToDictionaryAsync(item => item.TripId, item => item.Count, cancellationToken);

    public Task<List<Booking>> GetManifestAsync(Guid tripId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(booking => booking.Passenger)
            .Where(booking => booking.TripId == tripId && booking.Status != BookingStatus.Cancelled)
            .OrderBy(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
}
