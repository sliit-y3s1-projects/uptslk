using api.Entities.Enums;

namespace api.DTO.Centres;

public sealed class CentreFilter
{
    public CentreStatus? Status { get; init; }
    public string? District { get; init; }
    public string? Search { get; init; }
    public bool IncludeClosed { get; init; }
}
