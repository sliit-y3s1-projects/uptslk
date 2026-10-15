namespace api.Interfaces.Services;

// How a trip cancellation went. Card refunds that failed are listed so staff can retry them.
public sealed record TripCancellationResult(int CancelledBookings, IReadOnlyList<Guid> UnrefundedCardBookings);

// Cancels bookings and gives the passenger's money back.
public interface IBookingCancellationService
{
    // Wallet bookings are refunded to the wallet. Card bookings are refunded through the payment provider.
    Task CancelBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default);

    // Cancels the trip and every active booking on it.
    Task<TripCancellationResult> CancelTripAsync(Guid tripId, string reason, string? note = null, CancellationToken cancellationToken = default);
}
