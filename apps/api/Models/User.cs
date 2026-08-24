using Microsoft.AspNetCore.Identity;
using api.Enums;

namespace api.Models;

public class User : IdentityUser<Guid>
{

    public string Name { get; set; } = default!;
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