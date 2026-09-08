using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateFareRuleRequest
{
    public Guid RouteId { get; init; }
    public PassengerCategory PassengerCategory { get; init; }
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Amount { get; init; }
}

public sealed class UpdateFareRuleRequest
{
    public PassengerCategory PassengerCategory { get; init; }
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Amount { get; init; }
    public bool IsActive { get; init; }
}
