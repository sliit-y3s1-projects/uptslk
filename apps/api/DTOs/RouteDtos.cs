using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class RouteStopInput
{
    [Required, StringLength(160)] public string StopName { get; init; } = default!;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
}

public sealed class CreateRouteRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(32)] public string RouteNumber { get; init; } = default!;
    [Required, StringLength(200)] public string Name { get; init; } = default!;
    [Required, StringLength(160)] public string Origin { get; init; } = default!;
    [Required, StringLength(160)] public string Destination { get; init; } = default!;
    public VehicleType ServiceType { get; init; }
    [Range(0.1, 5000)] public decimal DistanceKm { get; init; }
    [Range(1, 1440)] public int EstimatedDurationMin { get; init; }
    [MinLength(2)] public List<RouteStopInput> Stops { get; init; } = [];
    public Guid? StartCentreId { get; init; }
    public Guid? EndCentreId { get; init; }
}

public class CreateRouteDirectionRequest
{
    public Guid StartCentreId { get; init; }
    public Guid EndCentreId { get; init; }
    [Required, StringLength(160)] public string Name { get; init; } = default!;
    [Range(0.1, 5000)] public decimal DistanceKm { get; init; }
    [Range(1, 1440)] public int EstimatedDurationMin { get; init; }
    [MinLength(2)] public List<RouteStopInput> Stops { get; init; } = [];
}

public sealed class UpdateRouteDirectionRequest : CreateRouteDirectionRequest
{
    public bool IsActive { get; init; } = true;
}

public sealed class UpdateRouteRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = default!;
    [Required, StringLength(160)] public string Origin { get; init; } = default!;
    [Required, StringLength(160)] public string Destination { get; init; } = default!;
    public VehicleType ServiceType { get; init; }
    [Range(0.1, 5000)] public decimal DistanceKm { get; init; }
    [Range(1, 1440)] public int EstimatedDurationMin { get; init; }
    [MinLength(2)] public List<RouteStopInput> Stops { get; init; } = [];
    public bool IsActive { get; init; } = true;
}

public sealed class CreateRouteScheduleRequest
{
    // A timetable belongs to one direction. Kept optional only while legacy
    // route-level timetable records are being migrated.
    public Guid? RouteDirectionId { get; init; }
    public Guid BayId { get; init; }
    public TimeOnly FirstDeparture { get; init; }
    public TimeOnly LastDeparture { get; init; }
    [Range(1, 720)] public int HeadwayMinutes { get; init; }
    [Required, StringLength(64)] public string OperatingDays { get; init; } = default!;
}

public sealed class UpdateRouteScheduleRequest
{
    public Guid BayId { get; init; }
    public TimeOnly FirstDeparture { get; init; }
    public TimeOnly LastDeparture { get; init; }
    [Range(1, 720)] public int HeadwayMinutes { get; init; }
    [Required, StringLength(64)] public string OperatingDays { get; init; } = default!;
    public bool IsActive { get; init; } = true;
}
