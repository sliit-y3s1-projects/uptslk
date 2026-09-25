namespace api.Models;

public class AgentStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = default!;

    public string AgentName { get; set; } = default!;
    public string InputJson { get; set; } = default!;   // stored as jsonb
    public string OutputJson { get; set; } = default!;  // stored as jsonb
    public string ToolCallsJson { get; set; } = "[]";  // stored as jsonb
    public string Status { get; set; } = "Completed";
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
