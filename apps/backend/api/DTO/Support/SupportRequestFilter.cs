using api.Entities.Enums;

namespace api.DTO.Support;

public sealed class SupportRequestFilter
{
    public SupportRequestType? Type { get; init; }
    public SupportRequestStatus? Status { get; init; }
    public Guid? PassengerId { get; init; }
    public Guid? TripId { get; init; }
}
