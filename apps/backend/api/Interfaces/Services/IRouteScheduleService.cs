using api.DTO.Routes;
using api.DTO.Trips;

namespace api.Interfaces.Services;

// Recurring timetables and the trips generated from them.
public interface IRouteScheduleService
{
    Task<IReadOnlyList<RouteScheduleResponse>> ListAsync(Guid routeId, CancellationToken cancellationToken);
    Task<RouteScheduleResponse> CreateAsync(Guid routeId, CreateRouteScheduleRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid scheduleId, UpdateRouteScheduleRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid scheduleId, CancellationToken cancellationToken);
    Task<GeneratedTripsResponse> GenerateTripsAsync(Guid scheduleId, GenerateScheduleTripsRequest request, CancellationToken cancellationToken);
}
