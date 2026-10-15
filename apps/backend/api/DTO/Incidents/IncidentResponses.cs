using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Incidents;

public sealed record IncidentListItemResponse(
    Guid Id,
    Guid CentreId,
    Guid? TripId,
    DateTime? TripTime,
    string? TripRouteNumber,
    string? TripRouteName,
    string ReportedByName,
    IncidentType Type,
    IncidentSeverity Severity,
    string Title,
    string Description,
    string? AssignedTo,
    IncidentStatus Status,
    DateTime SlaDueAt,
    DateTime? ResolvedAt,
    DateTime CreatedAt)
{
    public static IncidentListItemResponse From(Incident incident) => new(
        incident.Id,
        incident.CentreId,
        incident.TripId,
        incident.Trip?.ScheduledTime,
        incident.Trip?.Route.RouteNumber,
        incident.Trip?.Route.Name,
        incident.ReportedByName,
        incident.Type,
        incident.Severity,
        incident.Title,
        incident.Description,
        incident.AssignedTo,
        incident.Status,
        incident.SlaDueAt,
        incident.ResolvedAt,
        incident.CreatedAt);
}

public sealed record IncidentResponse(
    Guid Id,
    IncidentCentreSummary Centre,
    Guid? TripId,
    IncidentTripSummary? Trip,
    string ReportedByName,
    IncidentType Type,
    IncidentSeverity Severity,
    string Title,
    string Description,
    string? AssignedTo,
    IncidentStatus Status,
    DateTime SlaDueAt,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static IncidentResponse From(Incident incident) => new(
        incident.Id,
        new IncidentCentreSummary(incident.Centre.Id, incident.Centre.Code, incident.Centre.Name),
        incident.TripId,
        incident.Trip is null ? null : new IncidentTripSummary(incident.Trip.Id, incident.Trip.ScheduledTime, incident.Trip.Status),
        incident.ReportedByName,
        incident.Type,
        incident.Severity,
        incident.Title,
        incident.Description,
        incident.AssignedTo,
        incident.Status,
        incident.SlaDueAt,
        incident.ResolvedAt,
        incident.CreatedAt,
        incident.UpdatedAt);
}

public sealed record IncidentCentreSummary(Guid Id, string Code, string Name);

public sealed record IncidentTripSummary(Guid Id, DateTime ScheduledTime, TripStatus Status);

public sealed record IncidentCreatedResponse(Guid Id, Guid CentreId, Guid? TripId, string Title, IncidentStatus Status);
