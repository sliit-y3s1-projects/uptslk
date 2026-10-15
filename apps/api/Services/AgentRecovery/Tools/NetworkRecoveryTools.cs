using api.Interfaces.Repositories;

namespace api.Services.AgentRecovery.Tools;

public sealed class NetworkRecoveryTools(IBayRepository bays) : INetworkRecoveryTools
{
    public async Task<FindDepartureBayOutput> FindDepartureBayAsync(
        FindDepartureBayInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.DepartureCentreId, nameof(input.DepartureCentreId));
        RecoveryToolInputValidator.RequireId(input.ExcludedBayId, nameof(input.ExcludedBayId));

        // Available bays at the departure centre by code, without the bay that has the problem.
        var candidates = (await bays.ListAvailableInCentreAsync(input.DepartureCentreId, cancellationToken))
            .Where(bay => bay.Id != input.ExcludedBayId)
            .ToList();
        var candidate = candidates.FirstOrDefault();

        return new FindDepartureBayOutput(
            candidate?.Id,
            candidate?.Code,
            candidates.Count);
    }
}
