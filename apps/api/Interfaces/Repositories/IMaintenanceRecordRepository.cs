using api.DTO.Maintenance;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IMaintenanceRecordRepository : IRepository<MaintenanceRecord>
{
    Task<List<MaintenanceRecord>> SearchAsync(MaintenanceRecordFilter filter, CancellationToken cancellationToken = default);
    Task<MaintenanceRecord?> GetWithVehicleAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);

    // Active trips of the record's vehicle that fall on the maintenance day.
    Task<IReadOnlyList<Guid>> FindConflictingTripIdsAsync(MaintenanceRecord record, CancellationToken cancellationToken = default);
}
