namespace api.Services.AgentRecovery.Planning;

/// <summary>
/// Raised when the AI planner could not produce a valid plan and the caller did not
/// allow the safe built-in fallback plan to be used instead.
/// </summary>
public sealed class RecoveryPlanningUnavailableException(string reason) : Exception(reason);
