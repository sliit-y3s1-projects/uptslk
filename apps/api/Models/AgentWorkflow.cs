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
    public string? ProposalJson { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AgentStep> Steps { get; set; } = new List<AgentStep>();
    public ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();
}
