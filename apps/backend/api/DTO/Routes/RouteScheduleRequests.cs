using System.ComponentModel.DataAnnotations;

namespace api.DTO.Routes;

public sealed class CreateRouteScheduleRequest
{
    // A timetable belongs to one direction. It is optional only until old route-level timetables are moved over.
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
