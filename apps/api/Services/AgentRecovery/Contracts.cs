using api.Models;

namespace api.Services.AgentRecovery;

public sealed record RecoveryContext(AgentWorkflow Workflow, Trip Trip, int AffectedPassengers);

public sealed record AgentRecommendation(
    string AgentName,
    string Summary,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? BayId = null,
    DateTime? ScheduledTime = null,
    int? Capacity = null);

public sealed record RecoveryProposal(
    Guid VehicleId,
    Guid DriverId,
    Guid BayId,
    DateTime ScheduledTime,
    int AffectedPassengers,
    IReadOnlyList<string> Warnings);

public interface IRecoveryAgent
{
    string Name { get; }
    Task<AgentRecommendation> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken);
}
