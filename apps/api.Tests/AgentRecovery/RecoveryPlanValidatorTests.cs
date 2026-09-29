using api.Enums;
using api.Models;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;
using Microsoft.Extensions.Options;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryPlanValidatorTests
{
    private readonly RecoveryPlanValidator _validator = CreateValidator();

    [Fact]
    public void Validate_AcceptsSafeBreakdownPlan()
    {
        var plan = SafeRecoveryPlanFactory.Create("Restore the interrupted service safely.");

        var errors = _validator.Validate(plan, IncidentType.Breakdown);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_AcceptsFocusedDelayPlan()
    {
        var plan = new RecoveryPlanDraft(
            "Assess a short service delay.",
            [
                new PlannedRecoveryStep(
                    "network-check",
                    1,
                    RecoveryAgentId.NetworkContinuity,
                    "Check the revised departure time and bay.",
                    []),
                new PlannedRecoveryStep(
                    "passenger-check",
                    2,
                    RecoveryAgentId.PassengerFareImpact,
                    "Assess affected passengers and communication needs.",
                    ["network-check"])
            ],
            "A validated recommendation is ready for manager review.");

        var errors = _validator.Validate(plan, IncidentType.Delay);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RejectsDispatchWithoutFleetAssessment()
    {
        var plan = new RecoveryPlanDraft(
            "Prepare a dispatch change.",
            [
                new PlannedRecoveryStep(
                    "network-check",
                    1,
                    RecoveryAgentId.NetworkContinuity,
                    "Check the revised departure time and bay.",
                    []),
                new PlannedRecoveryStep(
                    "dispatch-check",
                    2,
                    RecoveryAgentId.DispatchRecovery,
                    "Find an available driver.",
                    ["network-check"]),
                new PlannedRecoveryStep(
                    "passenger-check",
                    3,
                    RecoveryAgentId.PassengerFareImpact,
                    "Assess affected passengers.",
                    ["dispatch-check"])
            ],
            "A validated recommendation is ready for manager review.");

        var errors = _validator.Validate(plan, IncidentType.Delay);

        Assert.Contains("Dispatch recovery requires a fleet proposal.", errors);
    }

    [Fact]
    public void Validate_RejectsUnknownAndForwardDependencies()
    {
        var plan = new RecoveryPlanDraft(
            "Assess the incident.",
            [
                new PlannedRecoveryStep(
                    "network-check",
                    1,
                    RecoveryAgentId.NetworkContinuity,
                    "Assess continuity.",
                    ["passenger-check", "missing-step"]),
                new PlannedRecoveryStep(
                    "passenger-check",
                    2,
                    RecoveryAgentId.PassengerFareImpact,
                    "Assess affected passengers.",
                    [])
            ],
            "A validated recommendation is ready for manager review.");

        var errors = _validator.Validate(plan, IncidentType.Delay);

        Assert.Contains("Dependency 'passenger-check' must run before step 'network-check'.", errors);
        Assert.Contains("Step 'network-check' references unknown dependency 'missing-step'.", errors);
    }

    [Fact]
    public void Validate_RejectsDispatchWithoutExplicitNetworkAndFleetDependencies()
    {
        var plan = new RecoveryPlanDraft(
            "Recover a breakdown.",
            [
                new PlannedRecoveryStep("network-check", 1, RecoveryAgentId.NetworkContinuity, "Assess continuity.", []),
                new PlannedRecoveryStep("fleet-check", 2, RecoveryAgentId.FleetReadiness, "Assess fleet.", []),
                new PlannedRecoveryStep("dispatch-check", 3, RecoveryAgentId.DispatchRecovery, "Assess dispatch.", []),
                new PlannedRecoveryStep("passenger-check", 4, RecoveryAgentId.PassengerFareImpact, "Assess passengers.", ["dispatch-check"])
            ],
            "A validated recommendation is ready for manager review.");

        var errors = _validator.Validate(plan, IncidentType.Breakdown);

        Assert.Contains("Dispatch recovery must depend on both the network and fleet assessment steps.", errors);
    }

    private static RecoveryPlanValidator CreateValidator()
    {
        IRecoveryAgent[] agents =
        [
            new StubAgent(RecoveryAgentId.NetworkContinuity),
            new StubAgent(RecoveryAgentId.FleetReadiness),
            new StubAgent(RecoveryAgentId.DispatchRecovery),
            new StubAgent(RecoveryAgentId.PassengerFareImpact)
        ];

        return new RecoveryPlanValidator(
            new RecoveryAgentRegistry(agents),
            Options.Create(new AgentAiOptions { MaximumPlanSteps = 4 }));
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
            throw new NotSupportedException("The validation tests do not execute agents.");
    }
}
