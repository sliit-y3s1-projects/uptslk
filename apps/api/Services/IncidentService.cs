using api.Common.Helpers;
using api.DTO.Incidents;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class IncidentService(
    IIncidentRepository incidents,
    ICentreRepository centres,
    ITripRepository trips,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMobileNotificationService notifications) : IIncidentService
{
    private const string NotFoundMessage = "Incident not found.";

    public async Task<IReadOnlyList<IncidentListItemResponse>> ListAsync(IncidentFilter filter, CancellationToken cancellationToken)
    {
        // Admins see every centre. Other staff only see their own centre.
        if (!currentUser.IsAdmin)
        {
            var ownCentreId = currentUser.CentreId ?? throw new ForbiddenException();
            if (filter.CentreId.HasValue && filter.CentreId != ownCentreId) throw new ForbiddenException();
            filter = filter with { CentreId = ownCentreId };
        }

        var found = await incidents.SearchAsync(filter, cancellationToken);
        return found.Select(IncidentListItemResponse.From).ToList();
    }

    public async Task<IncidentResponse> GetAsync(Guid incidentId, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetWithDetailsAsync(incidentId, cancellationToken);
        // Incidents from other centres are reported as missing so their ids are not revealed.
        if (incident is null || !currentUser.CanManageCentre(incident.CentreId)) throw new NotFoundException(NotFoundMessage);
        return IncidentResponse.From(incident);
    }

    public async Task<IncidentCreatedResponse> CreateAsync(CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanManageCentre(request.CentreId);
        if (!await centres.ExistsAsync(request.CentreId, cancellationToken))
            throw new BadRequestException("The selected centre does not exist.");
        if (request.TripId.HasValue && !await trips.ExistsInCentreAsync(request.TripId.Value, request.CentreId, cancellationToken))
            throw new BadRequestException("The selected trip does not belong to this centre.");

        var incident = new Incident
        {
            CentreId = request.CentreId,
            TripId = request.TripId,
            ReportedByName = request.ReportedByName.Trim(),
            Type = request.Type,
            Severity = request.Severity,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AssignedTo = Text.CleanOptional(request.AssignedTo),
            SlaDueAt = request.SlaDueAt ?? DateTime.UtcNow.AddHours(4)
        };
        incidents.Add(incident);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new IncidentCreatedResponse(incident.Id, incident.CentreId, incident.TripId, incident.Title, incident.Status);
    }

    public async Task UpdateAsync(Guid incidentId, UpdateIncidentRequest request, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetByIdAsync(incidentId, cancellationToken);
        if (incident is null || !currentUser.CanManageCentre(incident.CentreId)) throw new NotFoundException(NotFoundMessage);

        var previousStatus = incident.Status;
        incident.Severity = request.Severity;
        incident.Title = request.Title.Trim();
        incident.Description = request.Description.Trim();
        incident.AssignedTo = Text.CleanOptional(request.AssignedTo);
        incident.Status = request.Status;
        incident.SlaDueAt = request.SlaDueAt;
        incident.ResolvedAt = request.Status == IncidentStatus.Resolved ? DateTime.UtcNow : null;
        incident.UpdatedAt = DateTime.UtcNow;
        if (incident.Status != previousStatus) notifications.IncidentUpdated(incident);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
