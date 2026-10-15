using api.DTO.Drivers;

namespace api.Interfaces.Services;

// Driver records managed by centre staff.
public interface IDriverService
{
    Task<IReadOnlyList<DriverListItemResponse>> ListAsync(DriverFilter filter, CancellationToken cancellationToken);
    Task<DriverResponse> GetAsync(Guid driverId, CancellationToken cancellationToken);
    Task<DriverCreatedResponse> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid driverId, UpdateDriverRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid driverId, CancellationToken cancellationToken);
}
