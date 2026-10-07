using System.Security.Claims;

namespace api.Services;

public static class CentreAccess
{
    /// <summary>
    /// True for an Admin (works across every centre) or for staff whose centre_id claim equals the given centre.
    /// Staff without a centre assignment cannot change centre data.
    /// </summary>
    public static bool CanManageCentre(this ClaimsPrincipal user, Guid centreId) =>
        user.IsInRole("Admin")
        || (Guid.TryParse(user.FindFirstValue("centre_id"), out var assignedCentreId) && assignedCentreId == centreId);
}
