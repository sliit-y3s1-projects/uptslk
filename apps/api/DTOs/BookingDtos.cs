using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateBookingRequest
{
    public Guid TripId { get; init; }
    public Guid PassengerId { get; init; }
    [Required, StringLength(8)] public string SeatNumber { get; init; } = default!;
}

public sealed class CreateMyBookingRequest
{
    public Guid TripId { get; init; }
    [Required, StringLength(8)] public string SeatNumber { get; init; } = default!;
}

public sealed class ChangeBookingSeatRequest
{
    [Required, StringLength(8)] public string SeatNumber { get; init; } = default!;
}

public sealed class UpdateBookingStatusRequest
{
    public BookingStatus Status { get; init; }
}

public sealed class CancelBookingRequest
{
    [Required, StringLength(1000)] public string Reason { get; init; } = default!;
}
