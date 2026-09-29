using api.Enums;
using api.Models;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryPlanningServiceTests
{
    [Fact]
    public async Task CreateRevisedPlanAsync_ForwardsExecutionFeedbackOnce()
    {
        var planner = new RecordingPlanner(SafeRecoveryPlanFactory.Create("Restore service safely."));
        var service = CreateService(planner);
        var feedback = new[] { "No conflict-free alternate driver is available." };

        var result = await service.CreateRevisedPlanAsync(
            CreatePlanningInput(),
            feedback,
            CancellationToken.None);

        Assert.Equal("Gemini", result.Mode);
        Assert.Equal(feedback, planner.LastFeedback);
        Assert.Equal(1, planner.CallCount);
    }

    [Fact]
    public async Task CreateRevisedPlanAsync_RejectsMissingFailureEvidence()
    {
        var service = CreateService(new RecordingPlanner(SafeRecoveryPlanFactory.Create("Restore service safely.")));

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateRevisedPlanAsync(
            CreatePlanningInput(),
            [],
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateRevisedPlanAsync_UsesRecordedFallbackForInvalidRevision()
    {
        var invalidPlan = new RecoveryPlanDraft(
            "Invalid revision",
            [new PlannedRecoveryStep("network-check", 1, RecoveryAgentId.NetworkContinuity, "Assess continuity.", [])],
            "Incomplete");
        var service = CreateService(new RecordingPlanner(invalidPlan));

        var result = await service.CreateRevisedPlanAsync(
            CreatePlanningInput(),
            ["The first execution failed."],
            CancellationToken.None);

        Assert.Equal("Fallback", result.Mode);
        Assert.NotNull(result.FallbackReason);
        Assert.Equal(4, result.Plan.Steps.Count);
    }

    private static RecoveryPlanningService CreateService(IRecoveryPlanner planner)
    {
        IRecoveryAgent[] agents =
        [
            new StubAgent(RecoveryAgentId.NetworkContinuity),
            new StubAgent(RecoveryAgentId.FleetReadiness),
            new StubAgent(RecoveryAgentId.DispatchRecovery),
            new StubAgent(RecoveryAgentId.PassengerFareImpact)
        ];
        var options = Options.Create(new AgentAiOptions
        {
            Enabled = true,
            ApiKey = "test-only-key",
            MaxPlanningRetries = 0,
            MaximumPlanSteps = 4
        });
        var validator = new RecoveryPlanValidator(new RecoveryAgentRegistry(agents), options);
        return new RecoveryPlanningService(
            planner,
            validator,
            options,
            NullLogger<RecoveryPlanningService>.Instance);
    }

    private static RecoveryPlanningInput CreatePlanningInput() => new(
        Guid.NewGuid(),
        "Restore service safely.",
        new IncidentPlanningSnapshot(IncidentType.Breakdown, IncidentSeverity.High, "Engine fault", "Vehicle stopped."),
        new TripPlanningSnapshot(Guid.NewGuid(), Guid.NewGuid(), "EX-01", "Example route", "Origin to Destination", DateTime.UtcNow, 60),
        20,
        []);

    private sealed class RecordingPlanner(RecoveryPlanDraft plan) : IRecoveryPlanner
    {
        public int CallCount { get; private set; }
        public IReadOnlyCollection<string> LastFeedback { get; private set; } = [];

        public Task<RecoveryPlannerResponse> CreatePlanAsync(
            RecoveryPlanningInput input,
            IReadOnlyCollection<string> validationFeedback,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastFeedback = validationFeedback.ToArray();
            return Task.FromResult(new RecoveryPlannerResponse(plan, "test-model", 10, 5, 15));
        }
    }

    private sealed class StubAgent(RecoveryAgentId id) : IRecoveryAgent
    {
        public RecoveryAgentId Id { get; } = id;
        public string Name => Id.ToString();
        public string Responsibility => $"Test responsibility for {Id}.";
        public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>();

        public Task<AgentExecutionResult> AnalyseAsync(
            RecoveryContext context,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Planning tests do not execute agents.");
    }
}
