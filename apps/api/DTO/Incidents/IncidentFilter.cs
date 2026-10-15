using api.Entities.Enums;

namespace api.DTO.Incidents;

public sealed record IncidentFilter
{
    public Guid? CentreId { get; init; }
    public Guid? TripId { get; init; }
    public IncidentStatus? Status { get; init; }
    public IncidentType? Type { get; init; }
    public IncidentSeverity? Severity { get; init; }
}
