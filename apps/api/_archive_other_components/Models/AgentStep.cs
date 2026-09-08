namespace api.Models;

public class AgentStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = default!;

    public string AgentName { get; set; } = default!;
    public string InputJson { get; set; } = default!;   // stored as jsonb
    public string OutputJson { get; set; } = default!;  // stored as jsonb

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}