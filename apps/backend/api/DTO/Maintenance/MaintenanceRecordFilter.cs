using api.Entities.Enums;

namespace api.DTO.Maintenance;

public sealed class MaintenanceRecordFilter
{
    public Guid? VehicleId { get; init; }
    public Guid? CentreId { get; init; }
    public MaintenanceStatus? Status { get; init; }
}
