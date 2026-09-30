using api.Data;
using api.Enums;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Tools;

public sealed class FleetRecoveryTools(AppDbContext db) : IFleetRecoveryTools
{
    public async Task<FindReplacementVehicleOutput> FindReplacementVehicleAsync(
        FindReplacementVehicleInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.CentreId, nameof(input.CentreId));
        RecoveryToolInputValidator.RequireId(input.ExcludedVehicleId, nameof(input.ExcludedVehicleId));
        RecoveryToolInputValidator.RequireNonNegative(input.RequiredCapacity, nameof(input.RequiredCapacity));

        var vehicles = await db.Vehicles.AsNoTracking()
            .Include(vehicle => vehicle.MaintenanceRecords)
            .Where(vehicle => vehicle.CentreId == input.CentreId
                && vehicle.Id != input.ExcludedVehicleId
                && vehicle.Status == VehicleStatus.Active)
            .ToListAsync(cancellationToken);
        var candidates = vehicles
            .Where(vehicle => !vehicle.MaintenanceRecords.Any(record =>
                VehicleMaintenanceRules.BlocksTrip(record, input.ScheduledTime, input.DurationMinutes)))
            .OrderByDescending(vehicle => vehicle.Capacity >= input.RequiredCapacity)
            .ThenBy(vehicle => vehicle.Capacity)
            .Select(vehicle => new { vehicle.Id, vehicle.PlateNumber, vehicle.Capacity })
            .ToList();
        var candidate = candidates.FirstOrDefault();

        return new FindReplacementVehicleOutput(
            candidate?.Id,
            candidate?.PlateNumber,
            candidate?.Capacity,
            candidate is not null && candidate.Capacity >= input.RequiredCapacity,
            candidates.Count);
    }
}
