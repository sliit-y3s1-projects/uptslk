namespace api.DTO.Routes;

public sealed class RouteFilter
{
    public Guid? CentreId { get; init; }
    public string? Search { get; init; }
    public bool? Active { get; init; }
}
