using api.Entities.Enums;

namespace api.DTO.Passengers;

public sealed class PassengerFilter
{
    public PassengerCategory? Category { get; init; }
    public bool? Active { get; init; }
    public string? Search { get; init; }
}
