using System.Security.Claims;

namespace api.Services.AgentRecovery;

public sealed record RecoveryAccessScope(bool IsAdmin, Guid? CentreId)
{
    public bool CanAccess(Guid centreId) => IsAdmin || CentreId == centreId;

    public static bool TryCreate(
        ClaimsPrincipal principal,
        out RecoveryAccessScope scope)
    {
        if (principal.IsInRole("Admin"))
        {
            scope = new RecoveryAccessScope(true, null);
            return true;
        }

        if (Guid.TryParse(principal.FindFirstValue("centre_id"), out var centreId))
        {
            scope = new RecoveryAccessScope(false, centreId);
            return true;
        }

        scope = new RecoveryAccessScope(false, null);
        return false;
    }
}
