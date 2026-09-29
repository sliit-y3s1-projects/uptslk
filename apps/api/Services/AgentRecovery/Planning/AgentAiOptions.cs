namespace api.Services.AgentRecovery.Planning;

public sealed class AgentAiOptions
{
    public const string SectionName = "AgentAi";

    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Gemini";
    public string Model { get; set; } = "gemini-3.5-flash";
    public string PromptVersion { get; set; } = "recovery-planner-v1";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxPlanningRetries { get; set; } = 1;
    public int MaxWorkflowReplans { get; set; } = 1;
    public int MaximumPlanSteps { get; set; } = 8;
}
