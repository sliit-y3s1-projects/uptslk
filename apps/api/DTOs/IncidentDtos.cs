using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreateIncidentRequest
{
    public Guid CentreId { get; init; }
    public Guid? TripId { get; init; }
    [Required, StringLength(160)] public string ReportedByName { get; init; } = default!;
    public IncidentType Type { get; init; }
    public IncidentSeverity Severity { get; init; } = IncidentSeverity.Medium;
    [Required, StringLength(200)] public string Title { get; init; } = default!;
    [Required, StringLength(2000)] public string Description { get; init; } = default!;
    [StringLength(160)] public string? AssignedTo { get; init; }
    public DateTime? SlaDueAt { get; init; }
}

public sealed class UpdateIncidentRequest
{
    public IncidentSeverity Severity { get; init; }
    [Required, StringLength(200)] public string Title { get; init; } = default!;
    [Required, StringLength(2000)] public string Description { get; init; } = default!;
    [StringLength(160)] public string? AssignedTo { get; init; }
    public IncidentStatus Status { get; init; }
    public DateTime SlaDueAt { get; init; }
}
