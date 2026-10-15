using api.DTO.Vehicles;

namespace api.Interfaces.Services;

public interface IVehicleService
{
    Task<IReadOnlyList<VehicleListItemResponse>> ListAsync(VehicleFilter filter, CancellationToken cancellationToken);
    Task<VehicleResponse> GetAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<VehicleCreatedResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<VehicleImageResponse> UploadImageAsync(Guid vehicleId, byte[] content, string contentType, CancellationToken cancellationToken);
}
