using api.DTO.Incidents;

namespace api.Interfaces.Services;

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentListItemResponse>> ListAsync(IncidentFilter filter, CancellationToken cancellationToken);
    Task<IncidentResponse> GetAsync(Guid incidentId, CancellationToken cancellationToken);
    Task<IncidentCreatedResponse> CreateAsync(CreateIncidentRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid incidentId, UpdateIncidentRequest request, CancellationToken cancellationToken);
}
