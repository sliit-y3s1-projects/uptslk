using System.Security.Claims;

namespace api.Interfaces.Services;

// The signed-in user for the current request.
public interface ICurrentUserService
{
    ClaimsPrincipal Principal { get; }
    Guid? UserId { get; }
    Guid? CentreId { get; }
    bool IsAdmin { get; }
    bool IsInRole(string role);
    bool CanManageCentre(Guid centreId);

    // Returns the user id, or throws UnauthorizedException when nobody is signed in.
    Guid RequireUserId();

    // Throws ForbiddenException when the user cannot manage the given centre.
    void EnsureCanManageCentre(Guid centreId);
}
