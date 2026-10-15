using api.DTO.Vehicles;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IVehicleRepository : IRepository<Vehicle>
{
    // Loads each vehicle with its centre and only its open maintenance records.
    Task<List<Vehicle>> SearchAsync(VehicleFilter filter, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PlateNumberExistsAsync(string plateNumber, Guid? exceptVehicleId = null, CancellationToken cancellationToken = default);
}
