using api.DTO.Auth;

namespace api.Interfaces.Services;

// Staff and user accounts managed by an admin.
public interface IUserAdminService
{
    Task<UserSummaryResponse> CreateUserAsync(CreateUserRequest request);
    Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(bool staffOnly, CancellationToken cancellationToken);
    Task<UserSummaryResponse> AssignCentreAsync(Guid userId, AssignUserCentreRequest request, CancellationToken cancellationToken);
    Task<UserSummaryResponse> UpdateDetailsAsync(Guid userId, UpdateUserDetailsRequest request, CancellationToken cancellationToken);
    Task SetActiveAsync(Guid userId, bool isActive);
    Task ResetPasswordAsync(Guid userId, string newPassword);
}
