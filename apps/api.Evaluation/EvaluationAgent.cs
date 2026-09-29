using api.Services.AgentRecovery;

namespace Upts.AgentEvaluation;

internal sealed class EvaluationAgent(
    RecoveryAgentId id,
    string responsibility,
    params RecoveryToolName[] allowedTools) : IRecoveryAgent
{
    public RecoveryAgentId Id { get; } = id;
    public string Name { get; } = id.ToString();
    public string Responsibility { get; } = responsibility;
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = allowedTools.ToHashSet();

    public Task<AgentExecutionResult> AnalyseAsync(
        RecoveryContext context,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Evaluation capability agents are metadata-only and cannot execute tools.");
}
