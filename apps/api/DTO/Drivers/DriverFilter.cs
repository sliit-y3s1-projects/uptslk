using api.Entities.Enums;

namespace api.DTO.Drivers;

public sealed class DriverFilter
{
    public Guid? CentreId { get; init; }
    public DriverStatus? Status { get; init; }
    public string? Search { get; init; }
}
