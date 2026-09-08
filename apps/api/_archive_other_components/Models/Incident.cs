using api.Enums;

namespace api.Models;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CentreId { get; set; }
    public Centre Centre { get; set; } = default!;

    public Guid? ReportedById { get; set; }
    public User? ReportedBy { get; set; }
    public string ReportedByName { get; set; } = default!;

    public Guid? TripId { get; set; }
    public Trip? Trip { get; set; }

    public IncidentType Type { get; set; }
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? AssignedTo { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public DateTime SlaDueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
