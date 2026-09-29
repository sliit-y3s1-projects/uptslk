using api.Data;
using api.Enums;
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

        var candidates = await db.Vehicles.AsNoTracking()
            .Where(vehicle => vehicle.CentreId == input.CentreId
                && vehicle.Id != input.ExcludedVehicleId
                && vehicle.Status == VehicleStatus.Active
                && !vehicle.MaintenanceRecords.Any(record => record.Status == MaintenanceStatus.InProgress))
            .OrderByDescending(vehicle => vehicle.Capacity >= input.RequiredCapacity)
            .ThenBy(vehicle => vehicle.Capacity)
            .Select(vehicle => new { vehicle.Id, vehicle.PlateNumber, vehicle.Capacity })
            .ToListAsync(cancellationToken);
        var candidate = candidates.FirstOrDefault();

        return new FindReplacementVehicleOutput(
            candidate?.Id,
            candidate?.PlateNumber,
            candidate?.Capacity,
            candidate is not null && candidate.Capacity >= input.RequiredCapacity,
            candidates.Count);
    }
}
