using api.Enums;

namespace api.Models;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReportedById { get; set; }
    public User ReportedBy { get; set; } = default!;

    public Guid? TripId { get; set; }
    public Trip? Trip { get; set; }

    public IncidentType Type { get; set; }
    public string Description { get; set; } = default!;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public DateTime SlaDueAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}