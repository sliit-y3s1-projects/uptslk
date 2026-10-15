using api.Entities.Enums;

namespace api.DTO.Trips;

public sealed class TripFilter
{
    public Guid? CentreId { get; init; }
    // Trips that start or end at this centre.
    public Guid? TerminalId { get; init; }
    public Guid? RouteId { get; init; }
    public Guid? DirectionId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public Guid? BayId { get; init; }
    public TripStatus? Status { get; init; }
    // A Sri Lanka service date.
    public DateOnly? Date { get; init; }
    // Only future trips that can still take bookings.
    public bool BookableOnly { get; init; }
}
