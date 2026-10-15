using api.Data;
using api.DTO.Maintenance;
using api.Entities;
using api.Interfaces.Repositories;
using api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class MaintenanceRecordRepository(AppDbContext db) : Repository<MaintenanceRecord>(db), IMaintenanceRecordRepository
{
    public async Task<List<MaintenanceRecord>> SearchAsync(MaintenanceRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(record => record.Vehicle).ThenInclude(vehicle => vehicle.Centre).AsQueryable();
        if (filter.VehicleId.HasValue) query = query.Where(record => record.VehicleId == filter.VehicleId.Value);
        if (filter.CentreId.HasValue) query = query.Where(record => record.Vehicle.CentreId == filter.CentreId.Value);
        if (filter.Status.HasValue) query = query.Where(record => record.Status == filter.Status.Value);
        return await query.OrderBy(record => record.ScheduledFor).ToListAsync(cancellationToken);
    }

    public Task<MaintenanceRecord?> GetWithVehicleAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? Set : Set.AsNoTracking();
        return query.Include(record => record.Vehicle).SingleOrDefaultAsync(record => record.Id == id, cancellationToken);
    }

    public Task<List<MaintenanceRecord>> GetOpenForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(record => record.VehicleId == vehicleId
                && (record.Status == MaintenanceStatus.Scheduled || record.Status == MaintenanceStatus.InProgress))
            .ToListAsync(cancellationToken);
}
