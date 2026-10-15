using api.Data;
using api.DTO.Vehicles;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class VehicleRepository(AppDbContext db) : Repository<Vehicle>(db), IVehicleRepository
{
    public async Task<List<Vehicle>> SearchAsync(VehicleFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(vehicle => vehicle.Centre)
            .Include(vehicle => vehicle.MaintenanceRecords.Where(record =>
                record.Status != MaintenanceStatus.Completed && record.Status != MaintenanceStatus.Cancelled))
            .AsQueryable();
        if (filter.CentreId.HasValue) query = query.Where(vehicle => vehicle.CentreId == filter.CentreId.Value);
        if (filter.Status.HasValue) query = query.Where(vehicle => vehicle.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(vehicle => vehicle.PlateNumber.ToLower().Contains(term) || vehicle.Model.ToLower().Contains(term));
        }
        return await query.OrderBy(vehicle => vehicle.PlateNumber).ToListAsync(cancellationToken);
    }

    public Task<Vehicle?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(vehicle => vehicle.Centre)
            .Include(vehicle => vehicle.MaintenanceRecords)
            .SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<bool> PlateNumberExistsAsync(string plateNumber, Guid? exceptVehicleId = null, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(vehicle => vehicle.PlateNumber == plateNumber && vehicle.Id != exceptVehicleId, cancellationToken);
}
