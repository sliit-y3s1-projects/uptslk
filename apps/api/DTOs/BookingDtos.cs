using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateBookingRequest
{
    public Guid TripId { get; init; }
    public Guid PassengerId { get; init; }
    [Range(1, 10)] public int PassengerCount { get; init; } = 1;
}

public sealed class CreateMyBookingRequest
{
    public Guid TripId { get; init; }
    [Range(1, 10)] public int PassengerCount { get; init; } = 1;
}

public sealed class UpdateBookingStatusRequest
{
    public BookingStatus Status { get; init; }
}

public sealed class CancelBookingRequest
{
    [Required, StringLength(1000)] public string Reason { get; init; } = default!;
}
