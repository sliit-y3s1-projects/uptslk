using System.ComponentModel.DataAnnotations;
using api.Entities.Enums;

namespace api.DTO.Maintenance;

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
