using api.DTO.Bookings;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    // A commuter's bookings with everything a ticket shows, newest first.
    Task<List<Booking>> GetTicketsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Booking>> SearchAsync(BookingFilter filter, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithPassengerAsync(Guid id, CancellationToken cancellationToken = default);

    // Pending and confirmed bookings on the trip, with their passenger.
    Task<List<Booking>> GetActiveForTripWithPassengerAsync(Guid tripId, CancellationToken cancellationToken = default);
    // Loads the booking for cancelling, with its payments and the passenger's wallet.
    Task<Booking?> GetForCancellationAsync(Guid id, CancellationToken cancellationToken = default);

    // Loads the booking for a card refund, with its payments and their refunds.
    Task<Booking?> GetWithPaymentsAndRefundsAsync(Guid id, CancellationToken cancellationToken = default);

    // Ids of the pending and confirmed bookings on the trip.
    Task<List<Guid>> GetActiveIdsForTripAsync(Guid tripId, CancellationToken cancellationToken = default);
    // Active bookings on the trip that have not yet had an in-app notice from this recovery workflow, oldest first.
    Task<List<Booking>> GetActiveWithoutRecoveryNoticeAsync(Guid tripId, Guid workflowId, CancellationToken cancellationToken = default);
    Task<Booking?> GetWithTripAsync(Guid id, CancellationToken cancellationToken = default);

    // Seats taken by pending and confirmed bookings on the trip.
    Task<int> GetOccupiedCapacityAsync(Guid tripId, CancellationToken cancellationToken = default);

    // Seats taken on each trip. Trips with no bookings are left out.
    Task<Dictionary<Guid, int>> GetOccupiedCapacityByTripAsync(IReadOnlyCollection<Guid> tripIds, CancellationToken cancellationToken = default);

    // Bookings on the trip that are not cancelled, oldest first.
    Task<List<Booking>> GetManifestAsync(Guid tripId, CancellationToken cancellationToken = default);
}
