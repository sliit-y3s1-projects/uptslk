using System.Security.Claims;
using api.Common.Constants;

namespace api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Admins can manage every centre. Other staff can only manage the centre they are assigned to.
    public static bool CanManageCentre(this ClaimsPrincipal user, Guid centreId) =>
        user.IsInRole(AppRoles.Admin)
        || (Guid.TryParse(user.FindFirstValue(AppClaims.CentreId), out var assignedCentreId) && assignedCentreId == centreId);
}
