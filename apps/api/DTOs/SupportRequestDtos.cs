using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateSupportRequest
{
    public Guid? PassengerId { get; init; }
    public Guid? TripId { get; init; }
    public Guid? CentreId { get; init; }
    public SupportRequestType Type { get; init; } = SupportRequestType.Support;
    public SupportRequestPriority Priority { get; init; } = SupportRequestPriority.Medium;
    [Required, StringLength(200)] public string Subject { get; init; } = default!;
    [Required, StringLength(2000)] public string Description { get; init; } = default!;
    [StringLength(160)] public string? AssignedTo { get; init; }
}

public sealed class UpdateSupportRequest
{
    public SupportRequestPriority Priority { get; init; }
    public SupportRequestStatus Status { get; init; }
    [Required, StringLength(200)] public string Subject { get; init; } = default!;
    [Required, StringLength(2000)] public string Description { get; init; } = default!;
    [StringLength(160)] public string? AssignedTo { get; init; }
    [StringLength(2000)] public string? Resolution { get; init; }
}
