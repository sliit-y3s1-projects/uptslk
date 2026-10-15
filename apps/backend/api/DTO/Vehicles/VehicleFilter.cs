using api.Entities.Enums;

namespace api.DTO.Vehicles;

public sealed class VehicleFilter
{
    public Guid? CentreId { get; init; }
    public VehicleStatus? Status { get; init; }
    public string? Search { get; init; }
}
