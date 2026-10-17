using api.DTO.Trips;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface ITripRepository : IRepository<Trip>
{
    Task<List<Trip>> SearchAsync(TripFilter filter, CancellationToken cancellationToken = default);

    // Completed and cancelled trips, newest first.
    Task<List<Trip>> GetHistoryAsync(TripHistoryFilter filter, CancellationToken cancellationToken = default);
    Task<Trip?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetWithRouteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetWithRouteAndBayAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetWithVehicleAndRouteAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    // Trips that are not completed or cancelled, with their route and direction.
    Task<List<Trip>> GetActiveForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    // Active trips that use the vehicle, the driver or the bay, with their route.
    Task<List<Trip>> GetActiveUsingAnyAsync(Guid vehicleId, Guid driverId, Guid bayId, Guid? excludedTripId, CancellationToken cancellationToken = default);

    // Active trips that depart in [fromUtc, toUtc) from, to or through the centre, with route, vehicle, driver and bay.
    Task<List<Trip>> ListActiveInWindowAsync(Guid centreId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<bool> ExistsAtAsync(Guid routeId, Guid? directionId, DateTime scheduledTime, CancellationToken cancellationToken = default);
    Task<bool> ExistsInCentreAsync(Guid tripId, Guid centreId, CancellationToken cancellationToken = default);
    Task<Trip?> GetForDriverAsync(Guid tripId, Guid driverId, bool track = false, CancellationToken cancellationToken = default);

    // A driver's trips that depart in [startUtc, endUtc), with route, vehicle, bay and bookings.
    Task<List<Trip>> GetDriverDutiesAsync(Guid driverId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
