using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateTripRequest
{
    public Guid CentreId { get; init; }
    public Guid RouteId { get; init; }
    public Guid? RouteDirectionId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid DriverId { get; init; }
    public Guid BayId { get; init; }
    public DateTime ScheduledTime { get; init; }
    [StringLength(1000)] public string? Notes { get; init; }
}

public sealed class UpdateTripRequest
{
    public Guid CentreId { get; init; }
    public Guid RouteId { get; init; }
    public Guid? RouteDirectionId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid DriverId { get; init; }
    public Guid BayId { get; init; }
    public DateTime ScheduledTime { get; init; }
    [StringLength(1000)] public string? Notes { get; init; }
}

public sealed class ReassignTripRequest
{
    public Guid VehicleId { get; init; }
    public Guid DriverId { get; init; }
    public Guid BayId { get; init; }
    public DateTime ScheduledTime { get; init; }
    [StringLength(1000)] public string? Notes { get; init; }
}

public sealed class UpdateTripStatusRequest
{
    public TripStatus Status { get; init; }
    [StringLength(1000)] public string? Note { get; init; }
}

public sealed class CancelTripRequest
{
    [Required, StringLength(1000)] public string Reason { get; init; } = default!;
}

public sealed class GenerateScheduleTripsRequest
{
    public DateOnly ServiceDate { get; init; }
}
