using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Support;

public sealed record SupportRequestResponse(
    Guid Id,
    Guid? PassengerId,
    string? Passenger,
    Guid? TripId,
    string? Trip,
    Guid? CentreId,
    SupportRequestType Type,
    SupportRequestPriority Priority,
    SupportRequestStatus Status,
    string Subject,
    string Description,
    string? AssignedTo,
    string? Resolution,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt)
{
    public static SupportRequestResponse From(SupportRequest request) => new(
        request.Id,
        request.PassengerId,
        request.Passenger?.FullName,
        request.TripId,
        request.Trip?.Route.RouteNumber,
        request.CentreId,
        request.Type,
        request.Priority,
        request.Status,
        request.Subject,
        request.Description,
        request.AssignedTo,
        request.Resolution,
        request.CreatedAt,
        request.UpdatedAt,
        request.ResolvedAt);
}

public sealed record SupportRequestCreatedResponse(Guid Id, SupportRequestStatus Status);
