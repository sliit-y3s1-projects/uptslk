namespace api.Services.AgentRecovery.Agents;

public sealed class PassengerFareImpactAgent : IRecoveryAgent
{
    public string Name => "Passenger & Fare Impact Agent";

    public Task<AgentRecommendation> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var summary = context.AffectedPassengers == 0
            ? "There are no active passenger bookings to notify."
            : $"Notify {context.AffectedPassengers} affected passenger{(context.AffectedPassengers == 1 ? "" : "s")} about the proposed delay and replacement service.";
        return Task.FromResult(new AgentRecommendation(
            Name,
            summary,
            ["Confirmed and pending bookings are counted as active passenger impact.", "No automatic refund is recommended while a capacity-safe replacement is proposed."],
            []));
    }
}
