using System.Text.RegularExpressions;
using api.Common.Extensions;
using api.DTO.Auth;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Supabase.Storage.Exceptions;

namespace api.Services;

public sealed partial class AuthService(
    UserManager<User> userManager,
    IPassengerRepository passengers,
    IDriverRepository drivers,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuthCookieService cookies,
    JwtTokenService jwtService,
    IImageStorageService imageStorage,
    ILogger<AuthService> logger) : IAuthService
{
    public const int MaxPhotoBytes = 5 * 1024 * 1024;
    private const string InvalidCredentials = "Invalid credentials";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new User { UserName = request.Email, Email = request.Email, Name = request.Name, Role = UserRole.Commuter };
        (await userManager.CreateAsync(user, request.Password)).ThrowIfFailed();

        // Every commuter gets a passenger profile and an empty wallet. The phone number is filled in later.
        var passenger = new Passenger
        {
            UserId = user.Id,
            FullName = user.Name,
            PhoneNumber = $"pending-{user.Id:N}"[..28],
            Email = user.Email,
            Category = PassengerCategory.Adult
        };
        passenger.Wallet = new Wallet { Passenger = passenger };
        passengers.Add(passenger);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Do not leave a login account behind without its passenger profile.
            unitOfWork.DiscardChanges();
            await userManager.DeleteAsync(user);
            throw new BadRequestException("Unable to create the commuter profile. Ensure the latest database migration is applied.");
        }

        return SignIn(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new UnauthorizedException(InvalidCredentials);
        if (!user.IsActive) throw new ForbiddenException("Your account is temporarily disabled. Contact an administrator.");
        if (!await userManager.CheckPasswordAsync(user, request.Password)) throw new UnauthorizedException(InvalidCredentials);
        return SignIn(user);
    }

    public void Logout() => cookies.ClearSignInCookies();

    public async Task RefreshAsync()
    {
        var refreshToken = cookies.ReadRefreshToken();
        var userId = string.IsNullOrWhiteSpace(refreshToken) ? null : jwtService.ReadUserId(refreshToken);
        var user = userId.HasValue ? await userManager.FindByIdAsync(userId.Value.ToString()) : null;
        if (user is null || !user.IsActive) throw new UnauthorizedException();
        cookies.SetSignInCookies(user);
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync()
    {
        var user = await userManager.FindByIdAsync(currentUser.RequireUserId().ToString()) ?? throw new NotFoundException("User not found.");
        return UserProfileResponse.From(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await RequireCurrentUserAsync();
        (await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)).ThrowIfFailed();
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new BadRequestException("Name is required");
        var user = await RequireCurrentUserAsync();
        user.Name = request.Name.Trim();
        user.HomeLocation = request.HomeLocation?.Trim();
        user.NicNumber = request.NicNumber?.Trim();
        user.Gender = request.Gender?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        // Keep the linked passenger and driver records in step with the account.
        var passenger = await passengers.GetByUserIdAsync(user.Id, cancellationToken);
        if (passenger is not null)
        {
            passenger.FullName = user.Name;
            passenger.Email = user.Email;
        }
        var driver = await drivers.GetByUserIdAsync(user.Id, cancellationToken);
        if (driver is not null)
        {
            driver.FullName = user.Name;
            driver.UpdatedAt = DateTime.UtcNow;
        }

        // UserManager saves the whole context, so the passenger and driver changes are saved too.
        (await userManager.UpdateAsync(user)).ThrowIfFailed();
        return UserProfileResponse.From(user);
    }

    public async Task<ProfilePhotoResponse> UploadProfilePhotoAsync(byte[] content, string contentType, CancellationToken cancellationToken)
    {
        if (content.Length == 0) throw new BadRequestException("Select an image to upload.");
        if (content.Length > MaxPhotoBytes) throw new BadRequestException("Profile images cannot exceed 5 MB.");

        var user = await RequireCurrentUserAsync();
        try
        {
            user.ProfilePhotoUrl = await imageStorage.UploadProfileAsync(user.Id, content, contentType, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            throw new BadRequestException(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(exception, "Supabase profile-image storage is not configured");
            throw new ServiceUnavailableException("Configure Supabase storage before uploading profile images.");
        }
        catch (SupabaseStorageException exception)
        {
            logger.LogError(exception, "Supabase rejected the profile image for user {UserId}", user.Id);
            throw new BadGatewayException("The image could not be stored. Please try again.");
        }

        user.UpdatedAt = DateTime.UtcNow;
        (await userManager.UpdateAsync(user)).ThrowIfFailed();
        return new ProfilePhotoResponse(user.ProfilePhotoUrl);
    }

    public async Task<NicVerificationResponse> VerifyNicAsync(VerifyNicRequest request)
    {
        var nicNumber = request.NicNumber.Trim().ToUpperInvariant();
        var user = await RequireCurrentUserAsync();
        user.NicNumber = nicNumber;
        user.NicVerificationStatus = NicFormat().IsMatch(nicNumber) ? "Verified" : "Invalid";
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return new NicVerificationResponse(user.NicNumber, user.NicVerificationStatus, user.Gender);
    }

    private AuthResponse SignIn(User user)
    {
        var token = jwtService.GenerateToken(user);
        cookies.SetSignInCookies(user);
        return new AuthResponse(token, user.Id.ToString(), user.Name, user.Email!, user.Role.ToString(), user.CentreId, user.ProfilePhotoUrl);
    }

    private async Task<User> RequireCurrentUserAsync() =>
        await userManager.FindByIdAsync(currentUser.RequireUserId().ToString()) ?? throw new UnauthorizedException();

    // Old NICs are 9 digits and a V or X. New NICs are 12 digits.
    [GeneratedRegex("^(\\d{9}[VX]|\\d{12})$")]
    private static partial Regex NicFormat();
}
