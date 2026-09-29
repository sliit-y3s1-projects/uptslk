using api.Enums;

namespace api.Models;

public class AgentWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CentreId { get; set; }
    public Centre Centre { get; set; } = default!;

    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = default!;

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string Objective { get; set; } = default!;
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Running;
    public string PlanJson { get; set; } = "[]";
    public string PlanningMode { get; set; } = "Pending";
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public string PlannerInputJson { get; set; } = "{}";
    public string PlannerOutputJson { get; set; } = "{}";
    public string? PlanningFallbackReason { get; set; }
    public int PlanningDurationMs { get; set; }
    public int PromptTokenCount { get; set; }
    public int OutputTokenCount { get; set; }
    public int TotalTokenCount { get; set; }
    public string? ProposalJson { get; set; }
    public string ValidationJson { get; set; } = "[]";
    public string? FailureReason { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AgentStep> Steps { get; set; } = new List<AgentStep>();
    public ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();
}
