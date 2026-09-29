using System.Text.RegularExpressions;
using api.Enums;
using Microsoft.Extensions.Options;

namespace api.Services.AgentRecovery.Planning;

public sealed partial class RecoveryPlanValidator(
    RecoveryAgentRegistry registry,
    IOptions<AgentAiOptions> options)
{
    public IReadOnlyList<string> Validate(RecoveryPlanDraft plan, IncidentType incidentType)
    {
        var errors = new List<string>();
        var steps = plan.Steps ?? [];

        if (string.IsNullOrWhiteSpace(plan.ObjectiveSummary))
            errors.Add("The plan must include an objective summary.");
        if (string.IsNullOrWhiteSpace(plan.CompletionCondition))
            errors.Add("The plan must include a completion condition.");
        if (steps.Count == 0)
            errors.Add("The plan must include at least one specialist step.");
        if (steps.Count > options.Value.MaximumPlanSteps)
            errors.Add($"The plan exceeds the maximum of {options.Value.MaximumPlanSteps} specialist steps.");

        var stepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var orders = new HashSet<int>();
        var agentIds = new HashSet<RecoveryAgentId>();
        var stepById = new Dictionary<string, PlannedRecoveryStep>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.StepId) || !StepIdPattern().IsMatch(step.StepId))
                errors.Add("Every step ID must start with a letter and contain only lowercase letters, numbers, or hyphens.");
            else if (!stepIds.Add(step.StepId))
                errors.Add($"Step ID '{step.StepId}' is duplicated.");
            else
                stepById[step.StepId] = step;

            if (step.Order < 1 || !orders.Add(step.Order))
                errors.Add($"Step order '{step.Order}' must be a unique positive number.");
            if (!registry.Contains(step.AgentId))
                errors.Add($"Agent '{step.AgentId}' is not registered.");
            if (!agentIds.Add(step.AgentId))
                errors.Add($"Agent '{step.AgentId}' may appear only once in a recovery plan.");
            if (string.IsNullOrWhiteSpace(step.Objective))
                errors.Add($"Step '{step.StepId}' must include an objective.");
        }

        foreach (var step in steps)
        {
            foreach (var dependencyId in step.DependsOn ?? [])
            {
                if (!stepById.TryGetValue(dependencyId, out var dependency))
                    errors.Add($"Step '{step.StepId}' references unknown dependency '{dependencyId}'.");
                else if (dependency.Order >= step.Order)
                    errors.Add($"Dependency '{dependencyId}' must run before step '{step.StepId}'.");
            }
        }

        Require(agentIds, RecoveryAgentId.NetworkContinuity, errors, "Every recovery must assess service continuity.");
        Require(agentIds, RecoveryAgentId.PassengerFareImpact, errors, "Every recovery must assess passenger impact.");

        if (incidentType is IncidentType.Breakdown or IncidentType.Safety)
        {
            Require(agentIds, RecoveryAgentId.FleetReadiness, errors, $"A {incidentType.ToString().ToLowerInvariant()} incident requires a fleet assessment.");
            Require(agentIds, RecoveryAgentId.DispatchRecovery, errors, $"A {incidentType.ToString().ToLowerInvariant()} incident requires a dispatch assessment.");
        }

        if (agentIds.Contains(RecoveryAgentId.DispatchRecovery))
        {
            Require(agentIds, RecoveryAgentId.NetworkContinuity, errors, "Dispatch recovery requires a network proposal.");
            Require(agentIds, RecoveryAgentId.FleetReadiness, errors, "Dispatch recovery requires a fleet proposal.");
        }

        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static void Require(
        IReadOnlySet<RecoveryAgentId> selectedAgents,
        RecoveryAgentId requiredAgent,
        ICollection<string> errors,
        string error)
    {
        if (!selectedAgents.Contains(requiredAgent)) errors.Add(error);
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{1,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex StepIdPattern();
}
