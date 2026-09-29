using api.Services.AgentRecovery.Tools;

namespace api.Services.AgentRecovery.Agents;

public sealed class FleetReadinessAgent(IFleetRecoveryTools tools) : IRecoveryAgent
{
    public RecoveryAgentId Id => RecoveryAgentId.FleetReadiness;
    public string Name => "Fleet Readiness Agent";
    public string Responsibility => "Find an active, maintenance-safe replacement vehicle with sufficient capacity.";
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>
    {
        RecoveryToolName.FindReplacementVehicle
    };

    public async Task<AgentExecutionResult> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var toolInput = new FindReplacementVehicleInput(
            context.Trip.CentreId,
            context.Trip.VehicleId,
            context.AffectedPassengers);
        var toolOutput = await tools.FindReplacementVehicleAsync(toolInput, cancellationToken);
        if (toolOutput.CandidateVehicleId is null || toolOutput.Capacity is null)
        {
            var unavailable = new AgentRecommendation(Name, "No roadworthy replacement bus is currently available.", [], ["The workflow cannot proceed until a Fleet Officer provides an active replacement bus."]);
            return new AgentExecutionResult(unavailable,
            [new AgentToolCall(RecoveryToolName.FindReplacementVehicle, toolInput, toolOutput)]);
        }

        var capacityWarning = !toolOutput.MeetsRequiredCapacity
            ? $"The replacement has {toolOutput.Capacity} spaces for {context.AffectedPassengers} affected passengers."
            : null;
        var recommendation = new AgentRecommendation(
            Name,
            $"Use {toolOutput.PlateNumber} ({toolOutput.Capacity} passenger capacity) as the replacement bus.",
            ["Vehicle is active at the affected trip's centre.", "Vehicle has no maintenance record currently in progress."],
            capacityWarning is null ? [] : [capacityWarning],
            VehicleId: toolOutput.CandidateVehicleId,
            Capacity: toolOutput.Capacity);
        return new AgentExecutionResult(recommendation,
        [new AgentToolCall(RecoveryToolName.FindReplacementVehicle, toolInput, toolOutput)]);
    }
}
