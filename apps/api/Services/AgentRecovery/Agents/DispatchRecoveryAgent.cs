using api.Services.AgentRecovery.Tools;

namespace api.Services.AgentRecovery.Agents;

public sealed class DispatchRecoveryAgent(IDispatchRecoveryTools tools) : IRecoveryAgent
{
    public RecoveryAgentId Id => RecoveryAgentId.DispatchRecovery;
    public string Name => "Dispatch Recovery Agent";
    public string Responsibility => "Find an active and conflict-free replacement driver for the recovery proposal.";
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>
    {
        RecoveryToolName.FindConflictFreeDriver
    };

    public async Task<AgentExecutionResult> AnalyseAsync(
        RecoveryContext context,
        CancellationToken cancellationToken)
    {
        var network = GetRequiredEvidence(context, RecoveryAgentId.NetworkContinuity);
        var fleet = GetRequiredEvidence(context, RecoveryAgentId.FleetReadiness);
        if (network.BayId is null || network.ScheduledTime is null || fleet.VehicleId is null)
            throw new InvalidOperationException("Dispatch recovery requires complete network and fleet recommendations.");

        var toolInput = new FindConflictFreeDriverInput(
            context.Trip.CentreId,
            context.Trip.DriverId,
            fleet.VehicleId.Value,
            network.BayId.Value,
            network.ScheduledTime.Value,
            context.Trip.RouteDirection?.EstimatedDurationMin ?? context.Trip.Route.EstimatedDurationMin,
            context.Trip.Id);
        var toolOutput = await tools.FindConflictFreeDriverAsync(toolInput, cancellationToken);

        if (toolOutput.CandidateDriverId is null)
        {
            var unavailable = new AgentRecommendation(
                Name,
                "No alternate active driver is currently available for the proposed recovery combination.",
                [],
                ["All eligible alternate drivers conflict with the proposed vehicle, bay, or departure time."]);
            return new AgentExecutionResult(unavailable,
            [new AgentToolCall(RecoveryToolName.FindConflictFreeDriver, toolInput, toolOutput)]);
        }

        var recommendation = new AgentRecommendation(
            Name,
            $"Assign {toolOutput.DriverName} to the recovered departure.",
            [
                "Driver is active and belongs to the affected trip's centre.",
                "Driver is conflict-free for the exact proposed vehicle, bay, and departure time."
            ],
            [],
            DriverId: toolOutput.CandidateDriverId);
        return new AgentExecutionResult(recommendation,
        [new AgentToolCall(RecoveryToolName.FindConflictFreeDriver, toolInput, toolOutput)]);
    }

    private static AgentRecommendation GetRequiredEvidence(
        RecoveryContext context,
        RecoveryAgentId agentId) =>
        context.Evidence.TryGetValue(agentId, out var recommendation)
            ? recommendation
            : throw new InvalidOperationException($"Dispatch recovery is missing required {agentId} evidence.");
}
