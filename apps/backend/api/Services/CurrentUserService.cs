using System.Security.Claims;
using api.Common.Constants;
using api.Common.Extensions;
using api.Exceptions;
using api.Interfaces.Services;

namespace api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public Guid? UserId => ParseGuid(ClaimTypes.NameIdentifier);

    public Guid? CentreId => ParseGuid(AppClaims.CentreId);

    public bool IsAdmin => Principal.IsInRole(AppRoles.Admin);

    public bool IsInRole(string role) => Principal.IsInRole(role);

    public bool CanManageCentre(Guid centreId) => Principal.CanManageCentre(centreId);

    public Guid RequireUserId() => UserId ?? throw new UnauthorizedException();

    public void EnsureCanManageCentre(Guid centreId)
    {
        if (!CanManageCentre(centreId)) throw new ForbiddenException();
    }

    private Guid? ParseGuid(string claimType) =>
        Guid.TryParse(Principal.FindFirstValue(claimType), out var value) ? value : null;
}
