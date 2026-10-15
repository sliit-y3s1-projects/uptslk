namespace api.DTO.Fares;

public sealed class FareRuleFilter
{
    public Guid? RouteId { get; init; }
    public Guid? CentreId { get; init; }
    public bool? Active { get; init; }
}
