using api.DTO.Trips;

namespace api.Interfaces.Services;

public interface ITripService
{
    Task<IReadOnlyList<TripListItemResponse>> ListAsync(TripFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<TripListItemResponse>> ListHistoryAsync(TripHistoryFilter filter, CancellationToken cancellationToken);
    Task<TripResponse> GetAsync(Guid tripId, CancellationToken cancellationToken);
    Task<TripCreatedResponse> CreateAsync(CreateTripRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid tripId, UpdateTripRequest request, CancellationToken cancellationToken);
    Task ReassignAsync(Guid tripId, ReassignTripRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(Guid tripId, UpdateTripStatusRequest request, CancellationToken cancellationToken);
    Task CancelAsync(Guid tripId, CancelTripRequest request, CancellationToken cancellationToken);
}
