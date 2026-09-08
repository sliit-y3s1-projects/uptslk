using System.ComponentModel.DataAnnotations.Schema;
using api.Enums;

namespace api.Models;

public class Vehicle
{
    public Guid VehicleId { get; set; } = Guid.NewGuid();

    // Compatibility accessor for existing code referencing vehicle.Id
    [NotMapped]
    public Guid Id
    {
        get => VehicleId;
        set => VehicleId = value;
    }

    public Guid CentreId { get; set; }
    public string? CentreName { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = default!;

    // Compatibility accessor for existing code referencing vehicle.PlateNumber
    [NotMapped]
    public string PlateNumber
    {
        get => RegistrationNumber;
        set => RegistrationNumber = value;
    }

    public string? Model { get; set; } = string.Empty;

    public VehicleType VehicleType { get; set; } = VehicleType.Normal;

    // Compatibility accessor for existing code referencing vehicle.Type
    [NotMapped]
    public VehicleType Type
    {
        get => VehicleType;
        set => VehicleType = value;
    }

    public int Capacity { get; set; }
    public bool IsAccessible { get; set; } = false;
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
