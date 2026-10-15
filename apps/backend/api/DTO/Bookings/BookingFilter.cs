using api.Entities.Enums;

namespace api.DTO.Bookings;

public sealed class BookingFilter
{
    public Guid? PassengerId { get; init; }
    public Guid? TripId { get; init; }
    public BookingStatus? Status { get; init; }
}
