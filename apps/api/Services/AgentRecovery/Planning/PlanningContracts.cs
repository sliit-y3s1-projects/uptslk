using api.Enums;

namespace api.Services.AgentRecovery.Planning;

public sealed record AgentCapability(
    RecoveryAgentId Id,
    string Responsibility,
    IReadOnlyCollection<RecoveryToolName> AllowedTools);

public sealed record IncidentPlanningSnapshot(
    IncidentType Type,
    IncidentSeverity Severity,
    string Title,
    string Description);

public sealed record TripPlanningSnapshot(
    Guid TripId,
    Guid CentreId,
    string RouteNumber,
    string RouteName,
    string? Direction,
    DateTime ScheduledTime,
    int EstimatedDurationMinutes);

public sealed record RecoveryPlanningInput(
    Guid WorkflowId,
    string Objective,
    IncidentPlanningSnapshot Incident,
    TripPlanningSnapshot Trip,
    int AffectedPassengers,
    IReadOnlyCollection<AgentCapability> AvailableAgents);

public sealed record RecoveryPlanDraft(
    string ObjectiveSummary,
    IReadOnlyList<PlannedRecoveryStep> Steps,
    string CompletionCondition);

public sealed record PlannedRecoveryStep(
    string StepId,
    int Order,
    RecoveryAgentId AgentId,
    string Objective,
    IReadOnlyList<string> DependsOn);

public sealed record RecoveryPlannerResponse(
    RecoveryPlanDraft Plan,
    string Model,
    int PromptTokenCount,
    int OutputTokenCount,
    int TotalTokenCount);

public sealed record RecoveryPlanningResult(
    RecoveryPlanDraft Plan,
    string Mode,
    string Provider,
    string Model,
    string PromptVersion,
    string InputJson,
    string OutputJson,
    int DurationMs,
    int PromptTokenCount,
    int OutputTokenCount,
    int TotalTokenCount,
    string? FallbackReason);
