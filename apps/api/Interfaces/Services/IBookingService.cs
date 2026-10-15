using api.DTO.Bookings;

namespace api.Interfaces.Services;

public interface IBookingService
{
    Task<IReadOnlyList<TicketResponse>> ListTicketsForCurrentUserAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BookingListItemResponse>> ListAsync(BookingFilter filter, CancellationToken cancellationToken);
    Task<BookingResponse> GetAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<BookingCreatedResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);
    Task<BookingCreatedResponse> CreateForCurrentUserAsync(CreateMyBookingRequest request, CancellationToken cancellationToken);
    Task CompleteAsync(Guid bookingId, UpdateBookingStatusRequest request, CancellationToken cancellationToken);
    Task CancelAsync(Guid bookingId, CancelBookingRequest request, CancellationToken cancellationToken);
    Task<SeatAvailabilityResponse> GetSeatAvailabilityAsync(Guid tripId, CancellationToken cancellationToken);
    Task<TripManifestResponse> GetManifestAsync(Guid tripId, CancellationToken cancellationToken);
}
