using api.Interfaces.Repositories;

namespace api.Services.AgentRecovery.Tools;

public sealed class FleetRecoveryTools(IVehicleRepository vehicles) : IFleetRecoveryTools
{
    public async Task<FindReplacementVehicleOutput> FindReplacementVehicleAsync(
        FindReplacementVehicleInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.CentreId, nameof(input.CentreId));
        RecoveryToolInputValidator.RequireId(input.ExcludedVehicleId, nameof(input.ExcludedVehicleId));
        RecoveryToolInputValidator.RequireNonNegative(input.RequiredCapacity, nameof(input.RequiredCapacity));

        var activeVehicles = await vehicles.ListActiveInCentreWithMaintenanceAsync(input.CentreId, cancellationToken);

        // Prefer vehicles big enough for everyone, then the smallest of those, so large buses stay free.
        var candidates = activeVehicles
            .Where(vehicle => vehicle.Id != input.ExcludedVehicleId)
            .Where(vehicle => !vehicle.MaintenanceRecords.Any(record =>
                VehicleMaintenanceRules.BlocksTrip(record, input.ScheduledTime, input.DurationMinutes)))
            .OrderByDescending(vehicle => vehicle.Capacity >= input.RequiredCapacity)
            .ThenBy(vehicle => vehicle.Capacity)
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
