using api.Data;
using api.DTO.Drivers;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class DriverRepository(AppDbContext db) : Repository<Driver>(db), IDriverRepository
{
    public async Task<List<Driver>> SearchAsync(DriverFilter filter, CancellationToken cancellationToken = default)
    {
        var query = WithDetails();
        if (filter.CentreId.HasValue) query = query.Where(driver => driver.CentreId == filter.CentreId.Value);
        if (filter.Status.HasValue) query = query.Where(driver => driver.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(driver => driver.FullName.ToLower().Contains(term) || driver.LicenseNumber.ToLower().Contains(term));
        }
        return await query.OrderBy(driver => driver.FullName).ToListAsync(cancellationToken);
    }

    public Task<Driver?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithDetails().SingleOrDefaultAsync(driver => driver.Id == id, cancellationToken);

    public Task<List<Driver>> ListActiveInCentreAsync(Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(driver => driver.CentreId == centreId && driver.Status == DriverStatus.Active)
            .OrderBy(driver => driver.FullName)
            .ToListAsync(cancellationToken);

    public Task<Guid?> GetUserIdAsync(Guid driverId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Where(driver => driver.Id == driverId).Select(driver => driver.UserId).SingleOrDefaultAsync(cancellationToken);

    public Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.SingleOrDefaultAsync(driver => driver.UserId == userId, cancellationToken);

    public Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(driver => driver.LicenseNumber == licenseNumber, cancellationToken);

    public Task<Driver?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        WithDetails().SingleOrDefaultAsync(driver => driver.UserId == userId
            && driver.Status == DriverStatus.Active
            && driver.User != null && driver.User.IsActive, cancellationToken);

    private IQueryable<Driver> WithDetails() =>
        Set.AsNoTracking().Include(driver => driver.Centre).Include(driver => driver.User);
}
