using api.Services.AgentRecovery.Tools;

namespace api.Services.AgentRecovery.Agents;

public sealed class NetworkContinuityAgent(INetworkRecoveryTools tools) : IRecoveryAgent
{
    public RecoveryAgentId Id => RecoveryAgentId.NetworkContinuity;
    public string Name => "Network Continuity Agent";
    public string Responsibility => "Assess service continuity, departure resources, and a safe revised departure time.";
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>
    {
        RecoveryToolName.FindDepartureBay
    };

    public async Task<AgentExecutionResult> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var departureCentreId = context.Trip.RouteDirection?.StartCentreId ?? context.Trip.CentreId;
        var toolInput = new FindDepartureBayInput(departureCentreId, context.Trip.BayId);
        var toolOutput = await tools.FindDepartureBayAsync(toolInput, cancellationToken);
        var bayId = toolOutput.CandidateBayId ?? context.Trip.BayId;
        var departure = context.Trip.ScheduledTime.AddMinutes(15);
        var routeName = context.Trip.RouteDirection is null
            ? context.Trip.Route.Name
            : $"{context.Trip.RouteDirection.StartCentre.Name} to {context.Trip.RouteDirection.EndCentre.Name}";
        var recommendation = new AgentRecommendation(
            Name,
            toolOutput.CandidateBayId is null ? $"Keep the current bay and move {routeName} by 15 minutes." : $"Use bay {toolOutput.CandidateBayCode} and move {routeName} by 15 minutes.",
            ["The proposed departure is held 15 minutes later to give dispatch time to replace the affected resource.", "The bay belongs to the route direction's departure centre."],
            toolOutput.CandidateBayId is null ? ["No alternative available bay was found; dispatch must confirm the current bay can still be used."] : [],
            BayId: bayId,
            ScheduledTime: departure);
        return new AgentExecutionResult(recommendation,
        [
            new AgentToolCall(
                RecoveryToolName.FindDepartureBay,
                toolInput,
                toolOutput)
        ]);
    }
}
