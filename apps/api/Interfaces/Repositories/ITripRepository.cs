using api.Entities;

namespace api.Interfaces.Repositories;

public interface ITripRepository : IRepository<Trip>
{
    Task<Trip?> GetWithRouteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetWithVehicleAndRouteAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsInCentreAsync(Guid tripId, Guid centreId, CancellationToken cancellationToken = default);
    Task<Trip?> GetForDriverAsync(Guid tripId, Guid driverId, bool track = false, CancellationToken cancellationToken = default);

    // A driver's trips that depart in [startUtc, endUtc), with route, vehicle, bay and bookings.
    Task<List<Trip>> GetDriverDutiesAsync(Guid driverId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
