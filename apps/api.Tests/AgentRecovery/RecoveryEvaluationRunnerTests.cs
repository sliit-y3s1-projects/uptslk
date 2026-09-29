using api.Enums;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;
using Microsoft.Extensions.Options;
using Upts.AgentEvaluation;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryEvaluationRunnerTests
{
    [Fact]
    public async Task Baseline_ReportsValidPlansAndPreventsUnsafeActions()
    {
        var options = new AgentAiOptions { Enabled = false, MaximumPlanSteps = 8 };
        var registry = new RecoveryAgentRegistry(
        [
            new MetadataAgent(RecoveryAgentId.NetworkContinuity, RecoveryToolName.FindDepartureBay),
            new MetadataAgent(RecoveryAgentId.FleetReadiness, RecoveryToolName.FindReplacementVehicle),
            new MetadataAgent(RecoveryAgentId.DispatchRecovery, RecoveryToolName.FindConflictFreeDriver),
            new MetadataAgent(RecoveryAgentId.PassengerFareImpact, RecoveryToolName.AssessPassengerImpact)
        ]);
        var runner = new RecoveryEvaluationRunner(
            registry,
            new RecoveryPlanValidator(registry, Options.Create(options)),
            new RecoveryProposalComposer(),
            null,
            options,
            null,
            null);
        var scenarios = new[]
        {
            CreateScenario("safe", 45, ExpectedRecoveryOutcome.ApprovalReady),
            CreateScenario("unsafe", 20, ExpectedRecoveryOutcome.SafeFailure)
        };

        var report = await runner.RunAsync(scenarios, false, CancellationToken.None);

        Assert.Equal(1, report.Metrics.ValidPlanRate);
        Assert.Equal(1, report.Metrics.ExactAgentSelectionRate);
        Assert.Equal(1, report.Metrics.DeterministicOutcomeAccuracy);
        Assert.Equal(1, report.Metrics.UnsafeActionPreventionRate);
        Assert.Null(report.Metrics.EstimatedCostUsd);
    }

    private static RecoveryEvaluationScenario CreateScenario(
        string id,
        int replacementCapacity,
        ExpectedRecoveryOutcome expectedOutcome) => new(
        id,
        id,
        IncidentType.Breakdown,
        IncidentSeverity.High,
        "Engine fault",
        "The assigned vehicle cannot continue.",
        "Restore service safely.",
        40,
        [
            RecoveryAgentId.NetworkContinuity,
            RecoveryAgentId.FleetReadiness,
            RecoveryAgentId.DispatchRecovery,
            RecoveryAgentId.PassengerFareImpact
        ],
        expectedOutcome,
        new OperationalEvidenceFixture(true, true, replacementCapacity, true, true));

    private sealed class MetadataAgent(
        RecoveryAgentId id,
        RecoveryToolName tool) : IRecoveryAgent
    {
        public RecoveryAgentId Id { get; } = id;
        public string Name => Id.ToString();
        public string Responsibility => "Evaluation metadata.";
        public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName> { tool };

        public Task<AgentExecutionResult> AnalyseAsync(
            RecoveryContext context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
