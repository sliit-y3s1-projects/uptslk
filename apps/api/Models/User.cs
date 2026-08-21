using api.Enums;

namespace api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Links this record to the identity managed by ThunderID
    public string ThunderSub { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Driver? Driver { get; set; }
    public Wallet? Wallet { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Incident> ReportedIncidents { get; set; } = new List<Incident>();
    public ICollection<ApprovalRequest> ReviewedApprovals { get; set; } = new List<ApprovalRequest>();
}