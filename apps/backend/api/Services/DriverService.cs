using api.Common.Extensions;
using api.Common.Helpers;
using api.DTO.Drivers;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace api.Services;

public sealed class DriverService(
    IDriverRepository drivers,
    ICentreRepository centres,
    IUnitOfWork unitOfWork,
    UserManager<User> userManager,
    ICurrentUserService currentUser) : IDriverService
{
    private const string NotFoundMessage = "Driver not found.";
    private const string CentreMissing = "The selected centre does not exist.";

    public async Task<IReadOnlyList<DriverListItemResponse>> ListAsync(DriverFilter filter, CancellationToken cancellationToken)
    {
        var found = await drivers.SearchAsync(filter, cancellationToken);
        return found.Select(DriverListItemResponse.From).ToList();
    }

    public async Task<DriverResponse> GetAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetWithDetailsAsync(driverId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        return DriverResponse.From(driver);
    }

    public async Task<DriverCreatedResponse> CreateAsync(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        if (!await centres.ExistsAsync(request.CentreId, cancellationToken)) throw new BadRequestException(CentreMissing);
        currentUser.EnsureCanManageCentre(request.CentreId);

        var licenseNumber = request.LicenseNumber.Trim().ToUpperInvariant();
        if (await drivers.LicenseNumberExistsAsync(licenseNumber, cancellationToken))
            throw new ConflictException("A driver with this license number already exists.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new ConflictException("An account with this email already exists.");

        // The login account and the driver record are created together or not at all.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var user = new User
        {
            UserName = email,
            Email = email,
            Name = request.FullName.Trim(),
            Role = UserRole.Driver,
            CentreId = request.CentreId,
            IsActive = request.Status == DriverStatus.Active
        };
        (await userManager.CreateAsync(user, request.Password)).ThrowIfFailed();

        var driver = new Driver
        {
            CentreId = request.CentreId,
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            PhoneNumber = Text.CleanOptional(request.PhoneNumber),
            LicenseNumber = licenseNumber,
            Status = request.Status
        };
        drivers.Add(driver);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DriverCreatedResponse(driver.Id, driver.CentreId, driver.FullName, driver.LicenseNumber, driver.Status, driver.UserId, user.Email);
    }

    public async Task UpdateAsync(Guid driverId, UpdateDriverRequest request, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetByIdAsync(driverId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        if (!await centres.ExistsAsync(request.CentreId, cancellationToken)) throw new BadRequestException(CentreMissing);
        // Moving a driver needs access to both the old and the new centre.
        currentUser.EnsureCanManageCentre(driver.CentreId);
        currentUser.EnsureCanManageCentre(request.CentreId);

        driver.CentreId = request.CentreId;
        driver.FullName = request.FullName.Trim();
        driver.PhoneNumber = Text.CleanOptional(request.PhoneNumber);
        driver.Status = request.Status;
        driver.UpdatedAt = DateTime.UtcNow;

        var user = await FindLinkedUserAsync(driver);
        if (user is not null)
        {
            user.Name = driver.FullName;
            user.CentreId = driver.CentreId;
            user.IsActive = driver.Status == DriverStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            (await userManager.UpdateAsync(user)).ThrowIfFailed();
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetByIdAsync(driverId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        currentUser.EnsureCanManageCentre(driver.CentreId);

        driver.Status = DriverStatus.Inactive;
        driver.UpdatedAt = DateTime.UtcNow;

        // A deactivated driver can no longer sign in.
        var user = await FindLinkedUserAsync(driver);
        if (user is not null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User?> FindLinkedUserAsync(Driver driver) =>
        driver.UserId.HasValue ? await userManager.FindByIdAsync(driver.UserId.Value.ToString()) : null;
}
