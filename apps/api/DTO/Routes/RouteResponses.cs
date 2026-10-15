using api.Entities;
using api.Entities.Enums;
using Route = api.Entities.Route;
using RouteDirection = api.Entities.RouteDirection;

namespace api.DTO.Routes;

public sealed record RouteSummaryResponse(
    Guid Id,
    Guid CentreId,
    string Centre,
    string RouteNumber,
    string Name,
    string Origin,
    string Destination,
    VehicleType ServiceType,
    decimal DistanceKm,
    int EstimatedDurationMin,
    bool IsActive,
    int StopCount,
    int ActiveScheduleCount,
    IEnumerable<RouteDirectionResponse> Directions)
{
    public static RouteSummaryResponse From(Route route) => new(
        route.Id,
        route.CentreId,
        route.Centre.Name,
        route.RouteNumber,
        route.Name,
        route.Origin,
        route.Destination,
        route.ServiceType,
        route.DistanceKm,
        route.EstimatedDurationMin,
        route.IsActive,
        route.Directions.Sum(direction => direction.Stops.Count),
        route.Directions.Sum(direction => direction.Schedules.Count(schedule => schedule.IsActive)),
        route.Directions.Select(RouteDirectionResponse.From).ToList());
}

public sealed record RouteResponse(
    Guid Id,
    Guid CentreId,
    RouteCentreSummary Centre,
    string RouteNumber,
    string Name,
    string Origin,
    string Destination,
    VehicleType ServiceType,
    decimal DistanceKm,
    int EstimatedDurationMin,
    bool IsActive,
    IEnumerable<RouteStopResponse> Stops,
    IEnumerable<RouteScheduleSummary> Schedules,
    IEnumerable<RouteDirectionResponse> Directions)
{
    public static RouteResponse From(Route route) => new(
        route.Id,
        route.CentreId,
        RouteCentreSummary.From(route.Centre),
        route.RouteNumber,
        route.Name,
        route.Origin,
        route.Destination,
        route.ServiceType,
        route.DistanceKm,
        route.EstimatedDurationMin,
        route.IsActive,
        RouteStopResponse.FromAll(route.Stops),
        RouteScheduleSummary.FromAll(route.Schedules),
        route.Directions.Select(RouteDirectionResponse.From).ToList());
}

public sealed record RouteDirectionResponse(
    Guid Id,
    Guid RouteId,
    Guid StartCentreId,
    RouteCentreSummary StartCentre,
    Guid EndCentreId,
    RouteCentreSummary EndCentre,
    string Name,
    decimal DistanceKm,
    int EstimatedDurationMin,
    bool IsActive,
    IEnumerable<RouteStopResponse> Stops,
    IEnumerable<RouteScheduleSummary> Schedules)
{
    public static RouteDirectionResponse From(RouteDirection direction) => new(
        direction.Id,
        direction.RouteId,
        direction.StartCentreId,
        RouteCentreSummary.From(direction.StartCentre),
        direction.EndCentreId,
        RouteCentreSummary.From(direction.EndCentre),
        direction.Name,
        direction.DistanceKm,
        direction.EstimatedDurationMin,
        direction.IsActive,
        RouteStopResponse.FromAll(direction.Stops),
        RouteScheduleSummary.FromAll(direction.Schedules));
}

public sealed record RouteCentreSummary(Guid Id, string Code, string Name)
{
    public static RouteCentreSummary From(Centre centre) => new(centre.Id, centre.Code, centre.Name);
}

public sealed record RouteStopResponse(Guid Id, string StopName, int SequenceOrder, decimal Latitude, decimal Longitude)
{
    public static IReadOnlyList<RouteStopResponse> FromAll(IEnumerable<RouteStop> stops) =>
        stops.OrderBy(stop => stop.SequenceOrder)
            .Select(stop => new RouteStopResponse(stop.Id, stop.StopName, stop.SequenceOrder, stop.Latitude, stop.Longitude))
            .ToList();
}

public sealed record RouteScheduleSummary(
    Guid Id,
    Guid? RouteDirectionId,
    Guid BayId,
    string BayCode,
    TimeOnly FirstDeparture,
    TimeOnly LastDeparture,
    int HeadwayMinutes,
    string OperatingDays,
    bool IsActive)
{
    public static IReadOnlyList<RouteScheduleSummary> FromAll(IEnumerable<RouteSchedule> schedules) =>
        schedules.OrderBy(schedule => schedule.FirstDeparture)
            .Select(schedule => new RouteScheduleSummary(
                schedule.Id, schedule.RouteDirectionId, schedule.BayId, schedule.Bay.Code, schedule.FirstDeparture,
                schedule.LastDeparture, schedule.HeadwayMinutes, schedule.OperatingDays, schedule.IsActive))
            .ToList();
}

public sealed record RouteCreatedResponse(Guid Id, Guid CentreId, string RouteNumber, string Name);
