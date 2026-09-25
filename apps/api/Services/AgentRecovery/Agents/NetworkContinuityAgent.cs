using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Agents;

public sealed class NetworkContinuityAgent(AppDbContext db) : IRecoveryAgent
{
    public string Name => "Network Continuity Agent";
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>
    {
        RecoveryToolName.FindDepartureBay
    };

    public async Task<AgentExecutionResult> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var departureCentreId = context.Trip.RouteDirection?.StartCentreId ?? context.Trip.CentreId;
        var candidate = await db.Bays.AsNoTracking()
            .Where(bay => bay.CentreId == departureCentreId && bay.Status == BayStatus.Available && bay.Id != context.Trip.BayId)
            .OrderBy(bay => bay.Code)
            .FirstOrDefaultAsync(cancellationToken);
        var bayId = candidate?.Id ?? context.Trip.BayId;
        var departure = context.Trip.ScheduledTime.AddMinutes(15);
        var routeName = context.Trip.RouteDirection is null
            ? context.Trip.Route.Name
            : $"{context.Trip.RouteDirection.StartCentre.Name} to {context.Trip.RouteDirection.EndCentre.Name}";
        var recommendation = new AgentRecommendation(
            Name,
            candidate is null ? $"Keep the current bay and move {routeName} by 15 minutes." : $"Use bay {candidate.Code} and move {routeName} by 15 minutes.",
            ["The proposed departure is held 15 minutes later to give dispatch time to replace the affected resource.", "The bay belongs to the route direction's departure centre."],
            candidate is null ? ["No alternative available bay was found; dispatch must confirm the current bay can still be used."] : [],
            BayId: bayId,
            ScheduledTime: departure);
        return new AgentExecutionResult(recommendation,
        [
            new AgentToolCall(
                RecoveryToolName.FindDepartureBay,
                new { DepartureCentreId = departureCentreId, ExcludedBayId = context.Trip.BayId },
                new { CandidateBayId = candidate?.Id, CandidateBayCode = candidate?.Code, UsedCurrentBay = candidate is null })
        ]);
    }
}
