namespace api.Services.AgentRecovery.Planning;

public interface IRecoveryPlanner
{
    Task<RecoveryPlannerResponse> CreatePlanAsync(
        RecoveryPlanningInput input,
        IReadOnlyCollection<string> validationFeedback,
        CancellationToken cancellationToken);
}
