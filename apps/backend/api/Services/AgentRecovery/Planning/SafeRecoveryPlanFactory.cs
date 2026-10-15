namespace api.Services.AgentRecovery.Planning;

public static class SafeRecoveryPlanFactory
{
    public static RecoveryPlanDraft Create(string objective) => new(
        ObjectiveSummary: objective,
        Steps:
        [
            new PlannedRecoveryStep(
                "network-continuity",
                1,
                RecoveryAgentId.NetworkContinuity,
                "Assess the route direction, departure bay, and a safe revised departure time.",
                []),
            new PlannedRecoveryStep(
                "fleet-readiness",
                2,
                RecoveryAgentId.FleetReadiness,
                "Find an active, maintenance-safe vehicle with enough passenger capacity.",
                []),
            new PlannedRecoveryStep(
                "dispatch-recovery",
                3,
                RecoveryAgentId.DispatchRecovery,
                "Find a conflict-free driver for the proposed vehicle, bay, and time.",
                ["network-continuity", "fleet-readiness"]),
            new PlannedRecoveryStep(
                "passenger-impact",
                4,
                RecoveryAgentId.PassengerFareImpact,
                "Assess affected passengers, notification needs, and fare impact.",
                ["network-continuity", "fleet-readiness", "dispatch-recovery"])
        ],
        CompletionCondition: "A complete proposal passes deterministic safety checks and pauses for authorized manager approval.");
}
