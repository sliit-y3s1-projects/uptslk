using api.DTO.Drivers;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface IDriverRepository : IRepository<Driver>
{
    Task<List<Driver>> SearchAsync(DriverFilter filter, CancellationToken cancellationToken = default);
    Task<Driver?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Driver>> ListActiveInCentreAsync(Guid centreId, CancellationToken cancellationToken = default);
    Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken cancellationToken = default);

    // The active driver linked to an active user account.
    Task<Driver?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
