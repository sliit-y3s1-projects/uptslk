using System.ComponentModel.DataAnnotations;

namespace api.DTO.Fares;

public sealed class CreateFareRuleRequest
{
    public Guid RouteId { get; init; }
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Amount { get; init; }
}

public sealed class UpdateFareRuleRequest
{
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Amount { get; init; }
    public bool IsActive { get; init; }
}
