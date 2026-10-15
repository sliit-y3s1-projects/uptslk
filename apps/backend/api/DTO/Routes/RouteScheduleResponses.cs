using api.Entities;

namespace api.DTO.Routes;

public sealed record RouteScheduleResponse(
    Guid Id,
    Guid RouteId,
    Guid? RouteDirectionId,
    Guid BayId,
    string BayCode,
    TimeOnly FirstDeparture,
    TimeOnly LastDeparture,
    int HeadwayMinutes,
    string OperatingDays,
    bool IsActive)
{
    public static RouteScheduleResponse From(RouteSchedule schedule, Bay bay) => new(
        schedule.Id,
        schedule.RouteId,
        schedule.RouteDirectionId,
        schedule.BayId,
        bay.Code,
        schedule.FirstDeparture,
        schedule.LastDeparture,
        schedule.HeadwayMinutes,
        schedule.OperatingDays,
        schedule.IsActive);
}

// The result of turning one day of a timetable into trips.
public sealed record GeneratedTripsResponse(
    int Planned,
    int Created,
    int Existing,
    int Conflicts,
    DateOnly ServiceDate,
    IReadOnlyList<string> CreatedDepartures,
    IReadOnlyList<SkippedDeparture> SkippedDepartures);

public sealed record SkippedDeparture(string Time, string Reason);
