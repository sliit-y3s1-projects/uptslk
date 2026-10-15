using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Drivers;

public sealed record DriverListItemResponse(
    Guid Id,
    Guid CentreId,
    string Centre,
    string FullName,
    string? PhoneNumber,
    string LicenseNumber,
    DriverStatus Status,
    Guid? UserId,
    string? Email)
{
    public static DriverListItemResponse From(Driver driver) => new(
        driver.Id,
        driver.CentreId,
        driver.Centre.Name,
        driver.FullName,
        driver.PhoneNumber,
        driver.LicenseNumber,
        driver.Status,
        driver.UserId,
        driver.User?.Email);
}

public sealed record DriverResponse(
    Guid Id,
    Guid CentreId,
    DriverCentreSummary Centre,
    string FullName,
    string? PhoneNumber,
    string LicenseNumber,
    DriverStatus Status,
    Guid? UserId,
    string? Email)
{
    public static DriverResponse From(Driver driver) => new(
        driver.Id,
        driver.CentreId,
        new DriverCentreSummary(driver.Centre.Id, driver.Centre.Code, driver.Centre.Name),
        driver.FullName,
        driver.PhoneNumber,
        driver.LicenseNumber,
        driver.Status,
        driver.UserId,
        driver.User?.Email);
}

public sealed record DriverCentreSummary(Guid Id, string Code, string Name);

public sealed record DriverCreatedResponse(
    Guid Id,
    Guid CentreId,
    string FullName,
    string LicenseNumber,
    DriverStatus Status,
    Guid? UserId,
    string? Email);

public sealed record DriverProfileResponse(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    string LicenseNumber,
    DriverStatus Status,
    Guid CentreId,
    string CentreCode,
    string CentreName,
    string? Email)
{
    public static DriverProfileResponse From(Driver driver) => new(
        driver.Id,
        driver.FullName,
        driver.PhoneNumber,
        driver.LicenseNumber,
        driver.Status,
        driver.CentreId,
        driver.Centre.Code,
        driver.Centre.Name,
        driver.User!.Email);
}

public sealed record DriverTripResponse(
    Guid Id,
    Guid RouteId,
    Guid? RouteDirectionId,
    string RouteNumber,
    string RouteName,
    string Origin,
    string Destination,
    DateTime ScheduledTime,
    TripStatus Status,
    string VehiclePlate,
    string VehicleModel,
    int Capacity,
    string BayCode,
    string? BayName,
    int PassengerCount,
    string? Notes)
{
    public static DriverTripResponse From(Trip trip) => new(
        trip.Id,
        trip.RouteId,
        trip.RouteDirectionId,
        trip.Route.RouteNumber,
        trip.Route.Name,
        trip.RouteDirection?.StartCentre.Name ?? trip.Route.Origin,
        trip.RouteDirection?.EndCentre.Name ?? trip.Route.Destination,
        trip.ScheduledTime,
        trip.Status,
        trip.Vehicle.PlateNumber,
        trip.Vehicle.Model,
        trip.Vehicle.Capacity,
        trip.Bay.Code,
        trip.Bay.Name,
        trip.Bookings
            .Where(booking => booking.Status is BookingStatus.Pending or BookingStatus.Confirmed)
            .Sum(booking => booking.PassengerCount),
        trip.Notes);
}

public sealed record DriverIncidentCreatedResponse(Guid Id, Guid? TripId, IncidentStatus Status);
