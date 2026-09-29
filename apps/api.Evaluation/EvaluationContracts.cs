using api.Enums;
using api.Services.AgentRecovery;

namespace Upts.AgentEvaluation;

public enum ExpectedRecoveryOutcome
{
    ApprovalReady,
    SafeFailure
}

public sealed record OperationalEvidenceFixture(
    bool HasUsableBay,
    bool HasReplacementVehicle,
    int ReplacementCapacity,
    bool HasReplacementDriver,
    bool DispatchConflictFree);

public sealed record RecoveryEvaluationScenario(
    string Id,
    string Name,
    IncidentType IncidentType,
    IncidentSeverity Severity,
    string Title,
    string Description,
    string Objective,
    int AffectedPassengers,
    IReadOnlyList<RecoveryAgentId> ExpectedAgents,
    ExpectedRecoveryOutcome ExpectedOutcome,
    OperationalEvidenceFixture Evidence);

public sealed record EvaluationScenarioResult(
    string Id,
    string Name,
    bool PlanValid,
    IReadOnlyList<string> PlanErrors,
    IReadOnlyList<RecoveryAgentId> ExpectedAgents,
    IReadOnlyList<RecoveryAgentId> SelectedAgents,
    double AgentPrecision,
    double AgentRecall,
    bool ExactAgentSelection,
    ExpectedRecoveryOutcome ExpectedOutcome,
    ExpectedRecoveryOutcome ActualOutcome,
    bool OutcomeCorrect,
    IReadOnlyList<string> SafetyReasons,
    int PlanningDurationMs,
    int PromptTokenCount,
    int OutputTokenCount,
    int TotalTokenCount,
    decimal? EstimatedCostUsd);

public sealed record EvaluationMetrics(
    int ScenarioCount,
    double ValidPlanRate,
    double ExactAgentSelectionRate,
    double MeanAgentPrecision,
    double MeanAgentRecall,
    double DeterministicOutcomeAccuracy,
    double UnsafeActionPreventionRate,
    double ApprovalReadyRate,
    double SafeFailureRate,
    double AveragePlanningLatencyMs,
    int P95PlanningLatencyMs,
    int PromptTokenCount,
    int OutputTokenCount,
    int TotalTokenCount,
    decimal? EstimatedCostUsd);

public sealed record RecoveryEvaluationReport(
    string DatasetVersion,
    string EvaluationMode,
    string Planner,
    string PromptVersion,
    DateTime GeneratedAtUtc,
    EvaluationMetrics Metrics,
    IReadOnlyList<EvaluationScenarioResult> Scenarios);
