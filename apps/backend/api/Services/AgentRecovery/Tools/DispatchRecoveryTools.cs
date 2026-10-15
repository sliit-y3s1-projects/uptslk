using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services.AgentRecovery.Tools;

public sealed class DispatchRecoveryTools(
    IDriverRepository drivers,
    ITripConflictService conflictService) : IDispatchRecoveryTools
{
    public async Task<FindConflictFreeDriverOutput> FindConflictFreeDriverAsync(
        FindConflictFreeDriverInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.CentreId, nameof(input.CentreId));
        RecoveryToolInputValidator.RequireId(input.ExcludedDriverId, nameof(input.ExcludedDriverId));
        RecoveryToolInputValidator.RequireId(input.VehicleId, nameof(input.VehicleId));
        RecoveryToolInputValidator.RequireId(input.BayId, nameof(input.BayId));
        RecoveryToolInputValidator.RequireId(input.ExcludedTripId, nameof(input.ExcludedTripId));
        RecoveryToolInputValidator.RequirePositive(input.DurationMinutes, nameof(input.DurationMinutes));

        // Every other active driver at the centre, by name, so the choice is predictable.
        var candidates = (await drivers.ListActiveInCentreAsync(input.CentreId, cancellationToken))
            .Where(driver => driver.Id != input.ExcludedDriverId)
            .ToList();

        // The first driver with no clash wins.
        foreach (var candidate in candidates)
        {
            var conflicts = await conflictService.FindConflicts(
                input.VehicleId,
                candidate.Id,
                input.BayId,
                input.ScheduledTime,
                input.DurationMinutes,
                input.ExcludedTripId,
                cancellationToken);
            if (conflicts.Count == 0)
                return new FindConflictFreeDriverOutput(candidate.Id, candidate.FullName, candidates.Count);
        }

        return new FindConflictFreeDriverOutput(null, null, candidates.Count);
    }
}
