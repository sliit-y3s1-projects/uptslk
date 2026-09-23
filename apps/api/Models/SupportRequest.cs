using api.Enums;

namespace api.Models;

public class SupportRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PassengerId { get; set; }
    public Passenger? Passenger { get; set; }
    public Guid? TripId { get; set; }
    public Trip? Trip { get; set; }
    public Guid? CentreId { get; set; }
    public Centre? Centre { get; set; }
    public SupportRequestType Type { get; set; }
    public SupportRequestPriority Priority { get; set; } = SupportRequestPriority.Medium;
    public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Open;
    public string Subject { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? AssignedTo { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
