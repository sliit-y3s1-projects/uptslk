namespace api.Services.AgentRecovery.Planning;

// The AI planner gave no valid plan and the caller did not allow the safe built-in plan.
public sealed class RecoveryPlanningUnavailableException(string reason) : Exception(reason);
