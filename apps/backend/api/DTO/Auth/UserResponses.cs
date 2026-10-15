using api.Entities;

namespace api.DTO.Auth;

public sealed record UserProfileResponse(
    Guid Id,
    string Name,
    string? Email,
    string Role,
    Guid? CentreId,
    bool IsActive,
    string? HomeLocation,
    string? NicNumber,
    string? Gender,
    string? ProfilePhotoUrl,
    string NicVerificationStatus)
{
    public static UserProfileResponse From(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role.ToString(),
        user.CentreId,
        user.IsActive,
        user.HomeLocation,
        user.NicNumber,
        user.Gender,
        user.ProfilePhotoUrl,
        user.NicVerificationStatus);
}

public sealed record UserSummaryResponse(Guid Id, string Name, string? Email, string Role, Guid? CentreId, bool IsActive)
{
    public static UserSummaryResponse From(User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CentreId, user.IsActive);
}

public sealed record ProfilePhotoResponse(string? ProfilePhotoUrl);

public sealed record NicVerificationResponse(string? NicNumber, string NicVerificationStatus, string? Gender);
