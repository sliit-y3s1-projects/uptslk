using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services.AgentRecovery;

// The deterministic safety checks every recovery proposal must pass, before approval and again at approval.
public sealed class RecoveryProposalValidator(
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IBayRepository bays,
    ITripConflictService conflictService)
{
    public async Task<IReadOnlyList<ValidationResult>> ValidateAsync(Trip trip, RecoveryProposal proposal, string phase, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();
        var checkedAt = DateTime.UtcNow;
        // Read the resources fresh, because they may have changed since the agents looked at them.
        var vehicle = await vehicles.GetWithMaintenanceAsync(proposal.VehicleId, cancellationToken);
        var driver = await drivers.GetReadOnlyAsync(proposal.DriverId, cancellationToken);
        var bay = await bays.GetReadOnlyAsync(proposal.BayId, cancellationToken);
        results.Add(Check(phase, "Vehicle active at centre", vehicle is not null && vehicle.Status == VehicleStatus.Active && vehicle.CentreId == trip.CentreId, "The proposed vehicle is no longer active at this centre.", checkedAt));
        var maintenanceClear = vehicle is not null && !vehicle.MaintenanceRecords.Any(record =>
            VehicleMaintenanceRules.BlocksTrip(record, proposal.ScheduledTime,
                trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin));
        results.Add(Check(phase, "Vehicle maintenance clearance", maintenanceClear, "The proposed vehicle has scheduled or in-progress maintenance during this trip.", checkedAt));
        results.Add(Check(phase, "Driver active at centre", driver is not null && driver.Status == DriverStatus.Active && driver.CentreId == trip.CentreId, "The proposed driver is no longer active at this centre.", checkedAt));
        results.Add(Check(phase, "Bay available", bay is not null && bay.Status == BayStatus.Available, "The proposed bay is no longer available.", checkedAt));
        results.Add(Check(phase, "Vehicle capacity", vehicle is not null && vehicle.Capacity >= proposal.AffectedPassengers, "The proposed vehicle no longer has sufficient capacity.", checkedAt));
        if (vehicle is not null && driver is not null && bay is not null)
        {
            var conflicts = await conflictService.FindConflicts(vehicle.Id, driver.Id, bay.Id, proposal.ScheduledTime, trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin, trip.Id, cancellationToken);
            results.Add(Check(phase, "Vehicle, driver and bay conflicts", conflicts.Count == 0, conflicts.Count == 0 ? "No resource conflict was found." : string.Join(" ", conflicts), checkedAt));
        }
        else results.Add(Check(phase, "Vehicle, driver and bay conflicts", false, "Conflict validation could not run because a required resource is invalid.", checkedAt));
        return results;
    }

    private static ValidationResult Check(string phase, string check, bool passed, string failedDetail, DateTime checkedAt) =>
        new(phase, check, passed, passed ? $"{check} passed." : failedDetail, checkedAt);
}
