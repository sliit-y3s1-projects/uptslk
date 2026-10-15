namespace api.Interfaces.Services;

public interface IImageStorageService
{
    Task<string> UploadProfileAsync(
        Guid userId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> UploadVehicleAsync(
        Guid vehicleId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);
}
