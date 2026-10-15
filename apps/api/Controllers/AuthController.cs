using api.Common.Constants;
using api.DTO.Auth;
using api.Interfaces.Services;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService auth, IUserAdminService userAdmin) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request) =>
        Ok(await auth.LoginAsync(request));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> Me() =>
        Ok(await auth.GetCurrentUserAsync());

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        auth.Logout();
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        await auth.RefreshAsync();
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await auth.ChangePasswordAsync(request);
        return NoContent();
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.UpdateProfileAsync(request, cancellationToken));

    [HttpPost("me/profile-photo")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AuthService.MaxPhotoBytes + 1024 * 1024)]
    public async Task<ActionResult<ProfilePhotoResponse>> UploadProfilePhoto(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await auth.UploadProfilePhotoAsync(stream.ToArray(), file.ContentType, cancellationToken));
    }

    [HttpPost("me/verify-nic")]
    [Authorize]
    public async Task<ActionResult<NicVerificationResponse>> VerifyNic(VerifyNicRequest request) =>
        Ok(await auth.VerifyNicAsync(request));

    [HttpPost("create-user")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserSummaryResponse>> CreateUser(CreateUserRequest request) =>
        Ok(await userAdmin.CreateUserAsync(request));

    [HttpGet("users")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryResponse>>> Users([FromQuery] bool staffOnly, CancellationToken cancellationToken) =>
        Ok(await userAdmin.ListUsersAsync(staffOnly, cancellationToken));

    [HttpPatch("users/{id:guid}/centre")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserSummaryResponse>> AssignCentre(Guid id, AssignUserCentreRequest request, CancellationToken cancellationToken) =>
        Ok(await userAdmin.AssignCentreAsync(id, request, cancellationToken));

    [HttpPatch("users/{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<UserSummaryResponse>> UpdateUserDetails(Guid id, UpdateUserDetailsRequest request, CancellationToken cancellationToken) =>
        Ok(await userAdmin.UpdateDetailsAsync(id, request, cancellationToken));

    [HttpPatch("users/{id:guid}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] bool isActive)
    {
        await userAdmin.SetActiveAsync(id, isActive);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/reset-password")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] string newPassword)
    {
        await userAdmin.ResetPasswordAsync(id, newPassword);
        return NoContent();
    }
}
