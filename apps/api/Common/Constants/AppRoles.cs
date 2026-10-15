using api.Enums;

namespace api.Common.Constants;

// Role names for [Authorize] attributes. They always match the UserRole enum.
public static class AppRoles
{
    public const string Commuter = nameof(UserRole.Commuter);
    public const string Driver = nameof(UserRole.Driver);
    public const string Admin = nameof(UserRole.Admin);
    public const string CentreManager = nameof(UserRole.CentreManager);
    public const string Dispatcher = nameof(UserRole.Dispatcher);
    public const string FleetOfficer = nameof(UserRole.FleetOfficer);

    // Groups of roles that share the same access.
    public const string Managers = Admin + "," + CentreManager;
    public const string DispatchStaff = Managers + "," + Dispatcher;
    public const string FleetStaff = Managers + "," + FleetOfficer;
    public const string OperationsStaff = DispatchStaff + "," + FleetOfficer;
    public const string CommuterOrDriver = Commuter + "," + Driver;
}
