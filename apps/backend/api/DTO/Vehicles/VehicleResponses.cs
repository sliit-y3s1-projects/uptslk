using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Vehicles;

public sealed record VehicleListItemResponse(
    Guid Id,
    Guid CentreId,
    string Centre,
    string PlateNumber,
    string Model,
    VehicleType Type,
    int Capacity,
    bool IsAccessible,
    VehicleStatus Status,
    string? ImageUrl,
    int MaintenanceCount)
{
    // Expects Centre and only the open MaintenanceRecords to be loaded.
    public static VehicleListItemResponse From(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.CentreId,
        vehicle.Centre.Name,
        vehicle.PlateNumber,
        vehicle.Model,
        vehicle.Type,
        vehicle.Capacity,
        vehicle.IsAccessible,
        vehicle.Status,
        vehicle.ImageUrl,
        vehicle.MaintenanceRecords.Count);
}

public sealed record VehicleResponse(
    Guid Id,
    Guid CentreId,
    VehicleCentreSummary Centre,
    string PlateNumber,
    string Model,
    VehicleType Type,
    int Capacity,
    bool IsAccessible,
    VehicleStatus Status,
    string? ImageUrl,
    IEnumerable<VehicleMaintenanceSummary> Maintenance)
{
    public static VehicleResponse From(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.CentreId,
        new VehicleCentreSummary(vehicle.Centre.Id, vehicle.Centre.Code, vehicle.Centre.Name),
        vehicle.PlateNumber,
        vehicle.Model,
        vehicle.Type,
        vehicle.Capacity,
        vehicle.IsAccessible,
        vehicle.Status,
        vehicle.ImageUrl,
        vehicle.MaintenanceRecords
            .OrderByDescending(record => record.ScheduledFor)
            .Select(record => new VehicleMaintenanceSummary(record.Id, record.Type, record.Description, record.Status, record.ScheduledFor, record.CompletedAt))
            .ToList());
}

public sealed record VehicleCentreSummary(Guid Id, string Code, string Name);

public sealed record VehicleMaintenanceSummary(Guid Id, string Type, string Description, MaintenanceStatus Status, DateTime ScheduledFor, DateTime? CompletedAt);

public sealed record VehicleCreatedResponse(Guid Id, Guid CentreId, string PlateNumber, string Model, VehicleStatus Status, string? ImageUrl);

public sealed record VehicleImageResponse(string? ImageUrl);
