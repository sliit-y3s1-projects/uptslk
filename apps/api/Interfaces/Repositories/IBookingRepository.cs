using api.DTO.Bookings;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    // A commuter's bookings with everything a ticket shows, newest first.
    Task<List<Booking>> GetTicketsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Booking>> SearchAsync(BookingFilter filter, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithTripAsync(Guid id, CancellationToken cancellationToken = default);

    // Seats taken by pending and confirmed bookings on the trip.
    Task<int> GetOccupiedCapacityAsync(Guid tripId, CancellationToken cancellationToken = default);

    // Seats taken on each trip. Trips with no bookings are left out.
    Task<Dictionary<Guid, int>> GetOccupiedCapacityByTripAsync(IReadOnlyCollection<Guid> tripIds, CancellationToken cancellationToken = default);

    // Bookings on the trip that are not cancelled, oldest first.
    Task<List<Booking>> GetManifestAsync(Guid tripId, CancellationToken cancellationToken = default);
}
