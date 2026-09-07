using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateVehicleRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(32)] public string PlateNumber { get; init; } = default!;
    [Required, StringLength(120)] public string Model { get; init; } = default!;
    public VehicleType Type { get; init; }
    [Range(1, 200)] public int Capacity { get; init; }
    public bool IsAccessible { get; init; }
    public VehicleStatus Status { get; init; } = VehicleStatus.Active;
}

public sealed class UpdateVehicleRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(32)] public string PlateNumber { get; init; } = default!;
    [Required, StringLength(120)] public string Model { get; init; } = default!;
    public VehicleType Type { get; init; }
    [Range(1, 200)] public int Capacity { get; init; }
    public bool IsAccessible { get; init; }
    public VehicleStatus Status { get; init; }
}

public sealed class CreateDriverRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(160)] public string FullName { get; init; } = default!;
    [StringLength(32)] public string? PhoneNumber { get; init; }
    [Required, StringLength(64)] public string LicenseNumber { get; init; } = default!;
    public DriverStatus Status { get; init; } = DriverStatus.Active;
}

public sealed class UpdateDriverRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(160)] public string FullName { get; init; } = default!;
    [StringLength(32)] public string? PhoneNumber { get; init; }
    public DriverStatus Status { get; init; }
}

public sealed class CreateMaintenanceRecordRequest
{
    public Guid VehicleId { get; init; }
    [Required, StringLength(80)] public string Type { get; init; } = default!;
    [Required, StringLength(1000)] public string Description { get; init; } = default!;
    public DateTime ScheduledFor { get; init; }
}

public sealed class UpdateMaintenanceRecordRequest
{
    [Required, StringLength(80)] public string Type { get; init; } = default!;
    [Required, StringLength(1000)] public string Description { get; init; } = default!;
    public MaintenanceStatus Status { get; init; }
    public DateTime ScheduledFor { get; init; }
    public DateTime? CompletedAt { get; init; }
}
