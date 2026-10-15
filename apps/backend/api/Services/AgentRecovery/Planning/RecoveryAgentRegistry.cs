namespace api.Services.AgentRecovery.Planning;

public sealed class RecoveryAgentRegistry
{
    private readonly IReadOnlyDictionary<RecoveryAgentId, IRecoveryAgent> _agents;

    public RecoveryAgentRegistry(IEnumerable<IRecoveryAgent> agents)
    {
        var registeredAgents = agents.ToList();
        var duplicate = registeredAgents
            .GroupBy(agent => agent.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Recovery agent '{duplicate.Key}' is registered more than once.");

        _agents = registeredAgents.ToDictionary(agent => agent.Id);
    }

    public IReadOnlyCollection<AgentCapability> Capabilities => _agents.Values
        .OrderBy(agent => agent.Id)
        .Select(agent => new AgentCapability(agent.Id, agent.Responsibility, agent.AllowedTools.ToArray()))
        .ToArray();

    public bool Contains(RecoveryAgentId id) => _agents.ContainsKey(id);

    public IRecoveryAgent GetRequired(RecoveryAgentId id) =>
        _agents.TryGetValue(id, out var agent)
            ? agent
            : throw new InvalidOperationException($"Recovery agent '{id}' is not registered.");
}
