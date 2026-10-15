using api.Common.Extensions;
using api.DTO.Auth;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace api.Services;

public sealed class UserAdminService(
    UserManager<User> userManager,
    IUserRepository users,
    IPassengerRepository passengers,
    ICentreRepository centres) : IUserAdminService
{
    private const string NotFoundMessage = "User not found.";

    public async Task<UserSummaryResponse> CreateUserAsync(CreateUserRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role)) throw new BadRequestException("Invalid role");
        if (role == UserRole.Driver)
            throw new BadRequestException("Create driver accounts from Centre Ops so the login is linked to a driver record.");

        var user = new User { UserName = request.Email, Email = request.Email, Name = request.Name, Role = role, CentreId = request.CentreId };
        (await userManager.CreateAsync(user, request.Password)).ThrowIfFailed();
        return UserSummaryResponse.From(user);
    }

    public async Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(bool staffOnly, CancellationToken cancellationToken)
    {
        var found = await users.ListAsync(staffOnly, cancellationToken);
        return found.Select(UserSummaryResponse.From).ToList();
    }

    public async Task<UserSummaryResponse> AssignCentreAsync(Guid userId, AssignUserCentreRequest request, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId);
        if (user.Role is UserRole.Admin or UserRole.Commuter)
            throw new BadRequestException("Only staff accounts can be assigned to a centre.");
        if (request.CentreId.HasValue && !await centres.ExistsAsync(request.CentreId.Value, cancellationToken))
            throw new BadRequestException("The selected centre does not exist.");

        user.CentreId = request.CentreId;
        user.UpdatedAt = DateTime.UtcNow;
        (await userManager.UpdateAsync(user)).ThrowIfFailed();
        return UserSummaryResponse.From(user);
    }

    public async Task<UserSummaryResponse> UpdateDetailsAsync(Guid userId, UpdateUserDetailsRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            throw new BadRequestException("Name and email are required.");

        var user = await GetUserAsync(userId);
        var accountWithEmail = await userManager.FindByEmailAsync(email);
        if (accountWithEmail is not null && accountWithEmail.Id != user.Id)
            throw new ConflictException("Another account already uses this email address.");

        // The email is also the sign-in name.
        user.Name = name;
        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = userManager.NormalizeEmail(email);
        user.NormalizedUserName = userManager.NormalizeName(email);
        user.UpdatedAt = DateTime.UtcNow;

        var passenger = await passengers.GetByUserIdAsync(user.Id, cancellationToken);
        if (passenger is not null)
        {
            passenger.FullName = name;
            passenger.Email = email;
        }

        // UserManager saves the whole context, so the passenger change is saved too.
        (await userManager.UpdateAsync(user)).ThrowIfFailed();
        return UserSummaryResponse.From(user);
    }

    public async Task SetActiveAsync(Guid userId, bool isActive)
    {
        var user = await GetUserAsync(userId);
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword)) throw new BadRequestException("A new password is required");
        var user = await GetUserAsync(userId);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        (await userManager.ResetPasswordAsync(user, token, newPassword)).ThrowIfFailed();
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
    }

    private async Task<User> GetUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException(NotFoundMessage);
}
