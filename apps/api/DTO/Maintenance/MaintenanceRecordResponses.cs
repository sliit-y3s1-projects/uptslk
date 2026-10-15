using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Maintenance;

public sealed record MaintenanceRecordListItemResponse(
    Guid Id,
    Guid VehicleId,
    string Vehicle,
    Guid CentreId,
    string Centre,
    string Type,
    string Description,
    MaintenanceStatus Status,
    DateTime ScheduledFor,
    DateTime? CompletedAt)
{
    public static MaintenanceRecordListItemResponse From(MaintenanceRecord record) => new(
        record.Id,
        record.VehicleId,
        record.Vehicle.PlateNumber,
        record.Vehicle.CentreId,
        record.Vehicle.Centre.Name,
        record.Type,
        record.Description,
        record.Status,
        record.ScheduledFor,
        record.CompletedAt);
}

public sealed record MaintenanceRecordResponse(
    Guid Id,
    Guid VehicleId,
    MaintenanceVehicleSummary Vehicle,
    string Type,
    string Description,
    MaintenanceStatus Status,
    DateTime ScheduledFor,
    DateTime? CompletedAt)
{
    public static MaintenanceRecordResponse From(MaintenanceRecord record) => new(
        record.Id,
        record.VehicleId,
        new MaintenanceVehicleSummary(record.Vehicle.Id, record.Vehicle.PlateNumber, record.Vehicle.Model, record.Vehicle.CentreId),
        record.Type,
        record.Description,
        record.Status,
        record.ScheduledFor,
        record.CompletedAt);
}

public sealed record MaintenanceVehicleSummary(Guid Id, string PlateNumber, string Model, Guid CentreId);

public sealed record MaintenanceRecordCreatedResponse(Guid Id, Guid VehicleId, string Type, MaintenanceStatus Status, DateTime ScheduledFor);
