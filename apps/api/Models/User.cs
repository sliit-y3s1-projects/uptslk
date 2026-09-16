using Microsoft.AspNetCore.Identity;
using api.Enums;

namespace api.Models;

public class User : IdentityUser<Guid>
{

    public string Name { get; set; } = default!;
    public UserRole Role { get; set; }
    public Guid? CentreId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? HomeLocation { get; set; }
    public string? NicNumber { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public Passenger? Passenger { get; set; }
    public string NicVerificationStatus { get; set; } = "NotStarted";
    public Centre? Centre { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Driver? Driver { get; set; }
    public ICollection<Incident> ReportedIncidents { get; set; } = new List<Incident>();
    public ICollection<ApprovalRequest> ReviewedApprovals { get; set; } = new List<ApprovalRequest>();
}
