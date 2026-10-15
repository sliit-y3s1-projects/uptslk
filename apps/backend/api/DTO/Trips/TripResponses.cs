using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Trips;

public sealed record TripListItemResponse(
    Guid Id,
    Guid CentreId,
    Guid RouteId,
    Guid? RouteDirectionId,
    string RouteNumber,
    string RouteName,
    string? DirectionName,
    string Origin,
    string Destination,
    Guid VehicleId,
    string Vehicle,
    Guid DriverId,
    string Driver,
    Guid BayId,
    string Bay,
    DateTime ScheduledTime,
    TripStatus Status,
    string? Notes,
    int Capacity,
    int Occupied,
    int Available,
    bool IsFull)
{
    public static TripListItemResponse From(Trip trip, int occupied) => new(
        trip.Id,
        trip.CentreId,
        trip.RouteId,
        trip.RouteDirectionId,
        trip.Route.RouteNumber,
        trip.Route.Name,
        trip.RouteDirection?.Name,
        trip.RouteDirection?.StartCentre.Name ?? trip.Route.Origin,
        trip.RouteDirection?.EndCentre.Name ?? trip.Route.Destination,
        trip.VehicleId,
        trip.Vehicle.PlateNumber,
        trip.DriverId,
        trip.Driver.FullName,
        trip.BayId,
        trip.Bay.Code,
        trip.ScheduledTime,
        trip.Status,
        trip.Notes,
        trip.Vehicle.Capacity,
        occupied,
        Math.Max(0, trip.Vehicle.Capacity - occupied),
        occupied >= trip.Vehicle.Capacity);
}

public sealed record TripResponse(
    Guid Id,
    Guid CentreId,
    Guid RouteId,
    Guid? RouteDirectionId,
    string RouteNumber,
    string RouteName,
    string? DirectionName,
    Guid VehicleId,
    string Vehicle,
    Guid DriverId,
    string Driver,
    Guid BayId,
    string Bay,
    TripCentreSummary Centre,
    TripRouteSummary Route,
    TripVehicleSummary VehicleDetails,
    TripDriverSummary DriverDetails,
    TripBaySummary BayDetails,
    DateTime ScheduledTime,
    TripStatus Status,
    string? Notes,
    string? CancellationReason,
    DateTime? ActualDepartureAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static TripResponse From(Trip trip) => new(
        trip.Id,
        trip.CentreId,
        trip.RouteId,
        trip.RouteDirectionId,
        trip.Route.RouteNumber,
        trip.Route.Name,
        trip.RouteDirection?.Name,
        trip.VehicleId,
        trip.Vehicle.PlateNumber,
        trip.DriverId,
        trip.Driver.FullName,
        trip.BayId,
        trip.Bay.Code,
        new TripCentreSummary(trip.Centre.Id, trip.Centre.Code, trip.Centre.Name),
        new TripRouteSummary(
            trip.Route.Id,
            trip.Route.RouteNumber,
            trip.Route.Name,
            trip.RouteDirection?.StartCentre.Name ?? trip.Route.Origin,
            trip.RouteDirection?.EndCentre.Name ?? trip.Route.Destination,
            trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin),
        new TripVehicleSummary(trip.Vehicle.Id, trip.Vehicle.PlateNumber, trip.Vehicle.Model, trip.Vehicle.Capacity, trip.Vehicle.IsAccessible),
        new TripDriverSummary(trip.Driver.Id, trip.Driver.FullName, trip.Driver.LicenseNumber),
        new TripBaySummary(trip.Bay.Id, trip.Bay.Code, trip.Bay.Name),
        trip.ScheduledTime,
        trip.Status,
        trip.Notes,
        trip.CancellationReason,
        trip.ActualDepartureAt,
        trip.CompletedAt,
        trip.CreatedAt,
        trip.UpdatedAt);
}

public sealed record TripCentreSummary(Guid Id, string Code, string Name);

public sealed record TripRouteSummary(Guid Id, string RouteNumber, string Name, string Origin, string Destination, int EstimatedDurationMin);

public sealed record TripVehicleSummary(Guid Id, string PlateNumber, string Model, int Capacity, bool IsAccessible);

public sealed record TripDriverSummary(Guid Id, string FullName, string LicenseNumber);

public sealed record TripBaySummary(Guid Id, string Code, string? Name);

public sealed record TripCreatedResponse(Guid Id, Guid CentreId, Guid RouteId, DateTime ScheduledTime, TripStatus Status);
