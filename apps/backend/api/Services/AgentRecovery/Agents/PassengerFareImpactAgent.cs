using api.Services.AgentRecovery.Tools;

namespace api.Services.AgentRecovery.Agents;

public sealed class PassengerFareImpactAgent(IPassengerRecoveryTools tools) : IRecoveryAgent
{
    public RecoveryAgentId Id => RecoveryAgentId.PassengerFareImpact;
    public string Name => "Passenger & Fare Impact Agent";
    public string Responsibility => "Assess affected passengers, notification requirements, and fare impact.";
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>
    {
        RecoveryToolName.AssessPassengerImpact
    };

    public async Task<AgentExecutionResult> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var toolInput = new AssessPassengerImpactInput(context.Trip.Id);
        var toolOutput = await tools.AssessPassengerImpactAsync(toolInput, cancellationToken);
        var summary = toolOutput.AffectedPassengers == 0
            ? "There are no active passenger bookings to notify."
            : $"Notify {toolOutput.AffectedPassengers} affected passenger{(toolOutput.AffectedPassengers == 1 ? "" : "s")} about the proposed delay and replacement service.";
        var recommendation = new AgentRecommendation(
            Name,
            summary,
            ["Confirmed and pending bookings are counted as active passenger impact.", "No automatic refund is recommended while a capacity-safe replacement is proposed."],
            []);
        return new AgentExecutionResult(recommendation,
        [new AgentToolCall(RecoveryToolName.AssessPassengerImpact, toolInput, toolOutput)]);
    }
}
