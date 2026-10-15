using api.DTO.Vehicles;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Supabase.Storage.Exceptions;

namespace api.Services;

public sealed class VehicleService(
    IVehicleRepository vehicles,
    ICentreRepository centres,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IImageStorageService imageStorage,
    ILogger<VehicleService> logger) : IVehicleService
{
    public const int MaxImageBytes = 5 * 1024 * 1024;
    private const string NotFoundMessage = "Vehicle not found.";
    private const string DuplicatePlate = "A vehicle with this plate number already exists.";
    private const string CentreMissing = "The selected centre does not exist.";

    public async Task<IReadOnlyList<VehicleListItemResponse>> ListAsync(VehicleFilter filter, CancellationToken cancellationToken)
    {
        var found = await vehicles.SearchAsync(filter, cancellationToken);
        return found.Select(VehicleListItemResponse.From).ToList();
    }

    public async Task<VehicleResponse> GetAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetWithDetailsAsync(vehicleId, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return VehicleResponse.From(vehicle);
    }

    public async Task<VehicleCreatedResponse> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanManageCentre(request.CentreId);
        if (!await centres.ExistsAsync(request.CentreId, cancellationToken)) throw new BadRequestException(CentreMissing);

        var plateNumber = NormalizePlate(request.PlateNumber);
        if (await vehicles.PlateNumberExistsAsync(plateNumber, cancellationToken: cancellationToken)) throw new ConflictException(DuplicatePlate);

        var vehicle = new Vehicle
        {
            CentreId = request.CentreId,
            PlateNumber = plateNumber,
            Model = request.Model.Trim(),
            Type = request.Type,
            Capacity = request.Capacity,
            IsAccessible = request.IsAccessible,
            Status = request.Status
        };
        vehicles.Add(vehicle);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VehicleCreatedResponse(vehicle.Id, vehicle.CentreId, vehicle.PlateNumber, vehicle.Model, vehicle.Status, vehicle.ImageUrl);
    }

    public async Task UpdateAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await GetManagedVehicleAsync(vehicleId, cancellationToken);
        // Moving a vehicle needs access to both the old and the new centre.
        currentUser.EnsureCanManageCentre(request.CentreId);
        if (!await centres.ExistsAsync(request.CentreId, cancellationToken)) throw new BadRequestException(CentreMissing);

        var plateNumber = NormalizePlate(request.PlateNumber);
        if (await vehicles.PlateNumberExistsAsync(plateNumber, vehicleId, cancellationToken)) throw new ConflictException(DuplicatePlate);

        vehicle.CentreId = request.CentreId;
        vehicle.PlateNumber = plateNumber;
        vehicle.Model = request.Model.Trim();
        vehicle.Type = request.Type;
        vehicle.Capacity = request.Capacity;
        vehicle.IsAccessible = request.IsAccessible;
        vehicle.Status = request.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await GetManagedVehicleAsync(vehicleId, cancellationToken);
        vehicle.Status = VehicleStatus.Inactive;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<VehicleImageResponse> UploadImageAsync(Guid vehicleId, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        if (content.Length is 0 or > MaxImageBytes)
            throw new BadRequestException("Vehicle images must be between 1 byte and 5 MB.");

        var vehicle = await GetManagedVehicleAsync(vehicleId, cancellationToken);
        try
        {
            vehicle.ImageUrl = await imageStorage.UploadVehicleAsync(vehicle.Id, content, contentType, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            throw new BadRequestException(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(exception, "Vehicle image storage is not configured");
            throw new ServiceUnavailableException("Image storage is not configured.");
        }
        catch (SupabaseStorageException exception)
        {
            logger.LogError(exception, "Failed to upload an image for vehicle {VehicleId}", vehicleId);
            throw new BadGatewayException("The vehicle image could not be uploaded. Please try again.");
        }

        vehicle.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new VehicleImageResponse(vehicle.ImageUrl);
    }

    private async Task<Vehicle> GetManagedVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        currentUser.EnsureCanManageCentre(vehicle.CentreId);
        return vehicle;
    }

    private static string NormalizePlate(string value) => value.Trim().ToUpperInvariant();
}
