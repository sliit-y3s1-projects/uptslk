using api.Enums;

namespace api.Models;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CentreId { get; set; }
    public Centre Centre { get; set; } = default!;

    public string PlateNumber { get; set; } = default!;
    public string Model { get; set; } = default!;
    public VehicleType Type { get; set; }
    public int Capacity { get; set; }
    public bool IsAccessible { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
