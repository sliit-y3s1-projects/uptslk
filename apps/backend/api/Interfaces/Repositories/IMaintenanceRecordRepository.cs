using api.DTO.Maintenance;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IMaintenanceRecordRepository : IRepository<MaintenanceRecord>
{
    Task<List<MaintenanceRecord>> SearchAsync(MaintenanceRecordFilter filter, CancellationToken cancellationToken = default);
    Task<MaintenanceRecord?> GetWithVehicleAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);

    // Scheduled or in-progress maintenance for the vehicle.
    Task<List<MaintenanceRecord>> GetOpenForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
