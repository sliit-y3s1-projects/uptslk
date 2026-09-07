using api.Enums;

namespace api.Models;

public class MaintenanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
    public DateTime ScheduledFor { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
