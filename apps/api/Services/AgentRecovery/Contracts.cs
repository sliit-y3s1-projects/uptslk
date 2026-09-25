using api.Models;

namespace api.Services.AgentRecovery;

public sealed record RecoveryContext(AgentWorkflow Workflow, Trip Trip, int AffectedPassengers);

public enum RecoveryToolName
{
    FindDepartureBay,
    FindReplacementVehicle,
    FindConflictFreeDriver,
    AssessPassengerImpact
}

public sealed record AgentToolCall(
    RecoveryToolName Tool,
    object Input,
    object Output);

public sealed record AgentExecutionResult(
    AgentRecommendation Recommendation,
    IReadOnlyList<AgentToolCall> ToolCalls);

public sealed record RecoveryPlanStep(
    int Order,
    string Title,
    string Owner,
    string Purpose,
    string Status,
    DateTime? CompletedAt = null);

public sealed record ValidationResult(
    string Phase,
    string Check,
    bool Passed,
    string Detail,
    DateTime CheckedAt);

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
    IReadOnlySet<RecoveryToolName> AllowedTools { get; }
    Task<AgentExecutionResult> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken);
}
