using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Tools;

public sealed class NetworkRecoveryTools(AppDbContext db) : INetworkRecoveryTools
{
    public async Task<FindDepartureBayOutput> FindDepartureBayAsync(
        FindDepartureBayInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.DepartureCentreId, nameof(input.DepartureCentreId));
        RecoveryToolInputValidator.RequireId(input.ExcludedBayId, nameof(input.ExcludedBayId));

        var candidates = await db.Bays.AsNoTracking()
            .Where(bay => bay.CentreId == input.DepartureCentreId
                && bay.Status == BayStatus.Available
                && bay.Id != input.ExcludedBayId)
            .OrderBy(bay => bay.Code)
            .Select(bay => new { bay.Id, bay.Code })
            .ToListAsync(cancellationToken);
        var candidate = candidates.FirstOrDefault();

        return new FindDepartureBayOutput(
            candidate?.Id,
            candidate?.Code,
            candidates.Count);
    }
}
