using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Tools;

public sealed class DispatchRecoveryTools(
    AppDbContext db,
    TripConflictService conflictService) : IDispatchRecoveryTools
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

        var candidates = await db.Drivers.AsNoTracking()
            .Where(driver => driver.CentreId == input.CentreId
                && driver.Id != input.ExcludedDriverId
                && driver.Status == DriverStatus.Active)
            .OrderBy(driver => driver.FullName)
            .Select(driver => new { driver.Id, driver.FullName })
            .ToListAsync(cancellationToken);

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
