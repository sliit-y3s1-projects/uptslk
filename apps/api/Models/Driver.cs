using api.Enums;

namespace api.Models;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string LicenseNumber { get; set; } = default!;
    public DriverStatus Status { get; set; } = DriverStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}