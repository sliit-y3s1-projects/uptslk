using api.DTO.Maintenance;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class MaintenanceRecordService(
    IMaintenanceRecordRepository maintenanceRecords,
    IVehicleRepository vehicles,
    ITripRepository trips,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IMaintenanceRecordService
{
    private const string NotFoundMessage = "Maintenance record not found.";

    public async Task<IReadOnlyList<MaintenanceRecordListItemResponse>> ListAsync(MaintenanceRecordFilter filter, CancellationToken cancellationToken)
    {
        var records = await maintenanceRecords.SearchAsync(filter, cancellationToken);
        return records.Select(MaintenanceRecordListItemResponse.From).ToList();
    }

    public async Task<MaintenanceRecordResponse> GetAsync(Guid recordId, CancellationToken cancellationToken)
    {
        var record = await maintenanceRecords.GetWithVehicleAsync(recordId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return MaintenanceRecordResponse.From(record);
    }

    public async Task<MaintenanceRecordCreatedResponse> CreateAsync(CreateMaintenanceRecordRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new BadRequestException("The selected vehicle does not exist.");
        currentUser.EnsureCanManageCentre(vehicle.CentreId);

        var record = new MaintenanceRecord
        {
            VehicleId = request.VehicleId,
            Type = request.Type.Trim(),
            Description = request.Description.Trim(),
            ScheduledFor = request.ScheduledFor
        };
        await EnsureNoTripConflictsAsync(record, "Reassign or cancel the vehicle's active trips on this maintenance day before scheduling it.", cancellationToken);

        maintenanceRecords.Add(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MaintenanceRecordCreatedResponse(record.Id, record.VehicleId, record.Type, record.Status, record.ScheduledFor);
    }

    public async Task UpdateAsync(Guid recordId, UpdateMaintenanceRecordRequest request, CancellationToken cancellationToken)
    {
        var record = await GetManagedRecordAsync(recordId, cancellationToken);
        if (request.Status == MaintenanceStatus.Completed && request.CompletedAt is null)
            throw new BadRequestException("Completed maintenance requires a completion time.");

        // Check the new status and date before changing the tracked record.
        var proposed = new MaintenanceRecord { VehicleId = record.VehicleId, Status = request.Status, ScheduledFor = request.ScheduledFor };
        await EnsureNoTripConflictsAsync(proposed, "Reassign or cancel the vehicle's active trips before reserving it for maintenance.", cancellationToken);

        record.Type = request.Type.Trim();
        record.Description = request.Description.Trim();
        record.Status = request.Status;
        record.ScheduledFor = request.ScheduledFor;
        record.CompletedAt = request.CompletedAt;
        record.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid recordId, CancellationToken cancellationToken)
    {
        var record = await GetManagedRecordAsync(recordId, cancellationToken);
        record.Status = MaintenanceStatus.Cancelled;
        record.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<MaintenanceRecord> GetManagedRecordAsync(Guid recordId, CancellationToken cancellationToken)
    {
        var record = await maintenanceRecords.GetWithVehicleAsync(recordId, track: true, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        currentUser.EnsureCanManageCentre(record.Vehicle.CentreId);
        return record;
    }

    private async Task EnsureNoTripConflictsAsync(MaintenanceRecord record, string message, CancellationToken cancellationToken)
    {
        // Completed or cancelled work never blocks a trip.
        if (record.Status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled) return;

        var activeTrips = await trips.GetActiveForVehicleAsync(record.VehicleId, cancellationToken);
        var conflictingTripIds = activeTrips
            .Where(trip => VehicleMaintenanceRules.BlocksTrip(record, trip.ScheduledTime,
                trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin))
            .Select(trip => trip.Id)
            .ToList();
        if (conflictingTripIds.Count > 0)
            throw new ConflictException(message) { Details = { ["conflictingTripIds"] = conflictingTripIds } };
    }
}
