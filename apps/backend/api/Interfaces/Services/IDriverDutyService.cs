using api.DTO.Drivers;
using api.DTO.Incidents;
using api.DTO.Trips;

namespace api.Interfaces.Services;

// What a signed-in driver can see and do on their own trips.
public interface IDriverDutyService
{
    Task<DriverProfileResponse> GetProfileAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DriverTripResponse>> ListTripsAsync(DriverTripFilter filter, CancellationToken cancellationToken);
    Task UpdateTripStatusAsync(Guid tripId, UpdateTripStatusRequest request, CancellationToken cancellationToken);
    Task<DriverIncidentCreatedResponse> ReportIncidentAsync(Guid tripId, ReportDriverIncidentRequest request, CancellationToken cancellationToken);
}
