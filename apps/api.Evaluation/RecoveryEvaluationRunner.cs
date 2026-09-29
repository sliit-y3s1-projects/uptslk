using System.Diagnostics;
using api.Enums;
using api.Models;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;

namespace Upts.AgentEvaluation;

public sealed class RecoveryEvaluationRunner(
    RecoveryAgentRegistry registry,
    RecoveryPlanValidator validator,
    RecoveryProposalComposer proposalComposer,
    IRecoveryPlanner? livePlanner,
    AgentAiOptions options,
    decimal? inputCostPerMillionTokens,
    decimal? outputCostPerMillionTokens)
{
    public async Task<RecoveryEvaluationReport> RunAsync(
        IReadOnlyList<RecoveryEvaluationScenario> scenarios,
        bool useLivePlanner,
        CancellationToken cancellationToken)
    {
        if (scenarios.Count == 0)
            throw new InvalidOperationException("The evaluation dataset contains no scenarios.");
        if (useLivePlanner && livePlanner is null)
            throw new InvalidOperationException("Live evaluation requires GEMINI_API_KEY.");

        var results = new List<EvaluationScenarioResult>(scenarios.Count);
        foreach (var scenario in scenarios)
            results.Add(await EvaluateAsync(scenario, useLivePlanner, cancellationToken));

        return new RecoveryEvaluationReport(
            "recovery-evaluation-v1",
            useLivePlanner ? "gemini" : "deterministic-baseline",
            useLivePlanner ? options.Model : "safe-recovery-fallback",
            options.PromptVersion,
            DateTime.UtcNow,
            CalculateMetrics(results),
            results);
    }

    private async Task<EvaluationScenarioResult> EvaluateAsync(
        RecoveryEvaluationScenario scenario,
        bool useLivePlanner,
        CancellationToken cancellationToken)
    {
        var input = CreatePlanningInput(scenario, registry.Capabilities);
        var timer = Stopwatch.StartNew();
        RecoveryPlanDraft plan;
        var promptTokens = 0;
        var outputTokens = 0;
        var totalTokens = 0;

        if (useLivePlanner)
        {
            var response = await livePlanner!.CreatePlanAsync(input, [], cancellationToken);
            plan = response.Plan;
            promptTokens = response.PromptTokenCount;
            outputTokens = response.OutputTokenCount;
            totalTokens = response.TotalTokenCount;
        }
        else
        {
            plan = SafeRecoveryPlanFactory.Create(scenario.Objective);
        }
        timer.Stop();

        var planErrors = validator.Validate(plan, scenario.IncidentType);
        var selectedAgents = plan.Steps.Select(step => step.AgentId).Distinct().ToArray();
        var expectedAgents = scenario.ExpectedAgents.Distinct().ToArray();
        var intersectionCount = selectedAgents.Intersect(expectedAgents).Count();
        var precision = selectedAgents.Length == 0 ? 0 : (double)intersectionCount / selectedAgents.Length;
        var recall = expectedAgents.Length == 0 ? 1 : (double)intersectionCount / expectedAgents.Length;
        var exactSelection = selectedAgents.ToHashSet().SetEquals(expectedAgents);

        var composition = proposalComposer.Compose(
            CreateTrip(scenario),
            scenario.AffectedPassengers,
            plan,
            CreateRecommendations(scenario),
            []);
        var actualOutcome = composition.Proposal is null
            ? ExpectedRecoveryOutcome.SafeFailure
            : ExpectedRecoveryOutcome.ApprovalReady;
        var cost = CalculateCost(promptTokens, outputTokens);

        return new EvaluationScenarioResult(
            scenario.Id,
            scenario.Name,
            planErrors.Count == 0,
            planErrors,
            expectedAgents,
            selectedAgents,
            precision,
            recall,
            exactSelection,
            scenario.ExpectedOutcome,
            actualOutcome,
            actualOutcome == scenario.ExpectedOutcome,
            composition.Errors,
            (int)timer.ElapsedMilliseconds,
            promptTokens,
            outputTokens,
            totalTokens,
            cost);
    }

    private decimal? CalculateCost(int promptTokens, int outputTokens)
    {
        if (!inputCostPerMillionTokens.HasValue || !outputCostPerMillionTokens.HasValue)
            return null;

        return decimal.Round(
            promptTokens / 1_000_000m * inputCostPerMillionTokens.Value
            + outputTokens / 1_000_000m * outputCostPerMillionTokens.Value,
            6);
    }

    private static EvaluationMetrics CalculateMetrics(IReadOnlyList<EvaluationScenarioResult> results)
    {
        var expectedSafeFailures = results.Where(result => result.ExpectedOutcome == ExpectedRecoveryOutcome.SafeFailure).ToArray();
        var latencies = results.Select(result => result.PlanningDurationMs).Order().ToArray();
        var p95Index = Math.Max(0, (int)Math.Ceiling(latencies.Length * 0.95) - 1);
        var costs = results.Where(result => result.EstimatedCostUsd.HasValue).Select(result => result.EstimatedCostUsd!.Value).ToArray();

        return new EvaluationMetrics(
            results.Count,
            Rate(results.Count(result => result.PlanValid), results.Count),
            Rate(results.Count(result => result.ExactAgentSelection), results.Count),
            results.Average(result => result.AgentPrecision),
            results.Average(result => result.AgentRecall),
            Rate(results.Count(result => result.OutcomeCorrect), results.Count),
            expectedSafeFailures.Length == 0
                ? 1
                : Rate(expectedSafeFailures.Count(result => result.ActualOutcome == ExpectedRecoveryOutcome.SafeFailure), expectedSafeFailures.Length),
            Rate(results.Count(result => result.ActualOutcome == ExpectedRecoveryOutcome.ApprovalReady), results.Count),
            Rate(results.Count(result => result.ActualOutcome == ExpectedRecoveryOutcome.SafeFailure), results.Count),
            results.Average(result => result.PlanningDurationMs),
            latencies[p95Index],
            results.Sum(result => result.PromptTokenCount),
            results.Sum(result => result.OutputTokenCount),
            results.Sum(result => result.TotalTokenCount),
            costs.Length == results.Count ? costs.Sum() : null);
    }

    private static double Rate(int numerator, int denominator) =>
        denominator == 0 ? 0 : (double)numerator / denominator;

    private static RecoveryPlanningInput CreatePlanningInput(
        RecoveryEvaluationScenario scenario,
        IReadOnlyCollection<AgentCapability> capabilities) => new(
        Guid.NewGuid(),
        scenario.Objective,
        new IncidentPlanningSnapshot(
            scenario.IncidentType,
            scenario.Severity,
            scenario.Title,
            scenario.Description),
        new TripPlanningSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EVAL-01",
            "Evaluation route",
            "Origin to destination",
            DateTime.UtcNow.AddHours(1),
            60),
        scenario.AffectedPassengers,
        capabilities);

    private static Trip CreateTrip(RecoveryEvaluationScenario scenario)
    {
        var centreId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var bayId = Guid.NewGuid();
        return new Trip
        {
            Id = Guid.NewGuid(),
            CentreId = centreId,
            RouteId = Guid.NewGuid(),
            Route = new Route
            {
                RouteNumber = "EVAL-01",
                Name = "Evaluation route",
                Origin = "Origin",
                Destination = "Destination",
                EstimatedDurationMin = 60
            },
            VehicleId = vehicleId,
            Vehicle = new Vehicle
            {
                Id = vehicleId,
                CentreId = centreId,
                PlateNumber = "EVAL-CURRENT",
                Model = "Evaluation bus",
                Capacity = Math.Max(45, scenario.AffectedPassengers)
            },
            DriverId = driverId,
            Driver = new Driver { Id = driverId, CentreId = centreId, FullName = "Evaluation driver", LicenseNumber = "EVAL-LIC" },
            BayId = bayId,
            Bay = new Bay { Id = bayId, CentreId = centreId, Code = "EVAL-B01" },
            ScheduledTime = DateTime.UtcNow.AddHours(1),
            Status = TripStatus.Scheduled
        };
    }

    private static IReadOnlyDictionary<RecoveryAgentId, AgentRecommendation> CreateRecommendations(
        RecoveryEvaluationScenario scenario)
    {
        var bayId = scenario.Evidence.HasUsableBay ? Guid.NewGuid() : (Guid?)null;
        var vehicleId = scenario.Evidence.HasReplacementVehicle ? Guid.NewGuid() : (Guid?)null;
        var driverId = scenario.Evidence.HasReplacementDriver && scenario.Evidence.DispatchConflictFree
            ? Guid.NewGuid()
            : (Guid?)null;
        var scheduledTime = DateTime.UtcNow.AddHours(1).AddMinutes(15);

        return new Dictionary<RecoveryAgentId, AgentRecommendation>
        {
            [RecoveryAgentId.NetworkContinuity] = new(
                "Network Continuity Agent",
                "Evaluation network evidence.",
                [],
                [],
                BayId: bayId,
                ScheduledTime: scheduledTime),
            [RecoveryAgentId.FleetReadiness] = new(
                "Fleet Readiness Agent",
                "Evaluation fleet evidence.",
                [],
                [],
                VehicleId: vehicleId,
                Capacity: scenario.Evidence.HasReplacementVehicle ? scenario.Evidence.ReplacementCapacity : null),
            [RecoveryAgentId.DispatchRecovery] = new(
                "Dispatch Recovery Agent",
                "Evaluation dispatch evidence.",
                [],
                [],
                VehicleId: vehicleId,
                DriverId: driverId,
                BayId: bayId,
                ScheduledTime: scheduledTime),
            [RecoveryAgentId.PassengerFareImpact] = new(
                "Passenger and Fare Impact Agent",
                "Evaluation passenger evidence.",
                [],
                [])
        };
    }
}
