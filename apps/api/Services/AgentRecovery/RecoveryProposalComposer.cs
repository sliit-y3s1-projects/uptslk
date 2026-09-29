using api.Models;
using api.Services.AgentRecovery.Planning;

namespace api.Services.AgentRecovery;

public sealed record ProposalCompositionResult(
    RecoveryProposal? Proposal,
    IReadOnlyList<string> Errors);

public sealed class RecoveryProposalComposer
{
    private const int MaximumRecoveryDelayMinutes = 120;

    public ProposalCompositionResult Compose(
        Trip trip,
        int affectedPassengers,
        RecoveryPlanDraft plan,
        IReadOnlyDictionary<RecoveryAgentId, AgentRecommendation> recommendations,
        IReadOnlyCollection<string> executionErrors)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(recommendations);
        ArgumentNullException.ThrowIfNull(executionErrors);

        var errors = executionErrors.ToList();
        if (affectedPassengers < 0)
            errors.Add("Affected passenger count cannot be negative.");

        recommendations.TryGetValue(RecoveryAgentId.NetworkContinuity, out var network);
        recommendations.TryGetValue(RecoveryAgentId.FleetReadiness, out var fleet);
        recommendations.TryGetValue(RecoveryAgentId.DispatchRecovery, out var dispatch);
        recommendations.TryGetValue(RecoveryAgentId.PassengerFareImpact, out var passengerImpact);
        var selectedAgents = plan.Steps.Select(step => step.AgentId).ToHashSet();

        if (network?.BayId is null || network.BayId == Guid.Empty || network.ScheduledTime is null)
            errors.Add("The network assessment did not produce a usable departure bay and time.");
        if (passengerImpact is null)
            errors.Add("The passenger impact assessment did not complete.");

        var vehicleId = trip.VehicleId;
        var vehicleCapacity = trip.Vehicle.Capacity;
        if (selectedAgents.Contains(RecoveryAgentId.FleetReadiness))
        {
            if (fleet?.VehicleId is null || fleet.VehicleId == Guid.Empty || fleet.Capacity is null)
                errors.Add("No active, maintenance-safe replacement vehicle is available at this centre.");
            else
            {
                vehicleId = fleet.VehicleId.Value;
                vehicleCapacity = fleet.Capacity.Value;
                if (vehicleId == trip.VehicleId)
                    errors.Add("The fleet assessment selected the affected vehicle as its own replacement.");
            }
        }

        var driverId = trip.DriverId;
        if (selectedAgents.Contains(RecoveryAgentId.DispatchRecovery))
        {
            if (dispatch?.DriverId is null || dispatch.DriverId == Guid.Empty)
                errors.Add("No conflict-free alternate driver is available for the proposed recovery combination.");
            else
            {
                driverId = dispatch.DriverId.Value;
                if (driverId == trip.DriverId)
                    errors.Add("The dispatch assessment selected the affected driver as the alternate driver.");
                if (dispatch.VehicleId != vehicleId)
                    errors.Add("The dispatch driver was not checked against the proposed vehicle.");
                if (dispatch.BayId != network?.BayId)
                    errors.Add("The dispatch driver was not checked against the proposed departure bay.");
                if (dispatch.ScheduledTime != network?.ScheduledTime)
                    errors.Add("The dispatch driver was not checked against the proposed departure time.");
            }
        }

        if (vehicleCapacity < affectedPassengers)
            errors.Add($"The proposed vehicle capacity of {vehicleCapacity} cannot carry {affectedPassengers} affected passengers.");

        if (network?.ScheduledTime is DateTime proposedTime)
        {
            if (proposedTime < trip.ScheduledTime)
                errors.Add("The proposed recovery departure cannot be earlier than the original departure.");
            if (proposedTime > trip.ScheduledTime.AddMinutes(MaximumRecoveryDelayMinutes))
                errors.Add($"The proposed recovery departure exceeds the {MaximumRecoveryDelayMinutes}-minute recovery window.");
        }

        var distinctErrors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (distinctErrors.Length > 0)
            return new ProposalCompositionResult(null, distinctErrors);

        var warnings = recommendations.Values
            .SelectMany(recommendation => recommendation.Warnings)
            .Where(warning => !string.IsNullOrWhiteSpace(warning))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var proposal = new RecoveryProposal(
            vehicleId,
            driverId,
            network!.BayId!.Value,
            network.ScheduledTime!.Value,
            affectedPassengers,
            warnings);
        return new ProposalCompositionResult(proposal, []);
    }
}
