using api.DTO.Auth;

namespace api.Interfaces.Services;

// Signing up, signing in, and the signed-in user's own account.
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    void Logout();
    Task RefreshAsync();

    Task<UserProfileResponse> GetCurrentUserAsync();
    Task ChangePasswordAsync(ChangePasswordRequest request);
    Task<UserProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken);
    Task<ProfilePhotoResponse> UploadProfilePhotoAsync(byte[] content, string contentType, CancellationToken cancellationToken);
    Task<NicVerificationResponse> VerifyNicAsync(VerifyNicRequest request);
}
