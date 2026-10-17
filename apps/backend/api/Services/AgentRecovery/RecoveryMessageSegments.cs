using api.Entities;
using api.Entities.Enums;

namespace api.Services.AgentRecovery;

// Groups affected passengers by how they need to be told about the change.
public static class RecoveryMessageSegments
{
    public const string Priority = "priority";
    public const string App = "app";
    public const string NoAccount = "no-account";

    public static IReadOnlyList<string> All { get; } = [Priority, App, NoAccount];

    public static bool IsKnown(string segment) => All.Contains(segment);

    public static string SegmentOf(Booking booking) => SegmentOf(booking.Passenger);

    public static string SegmentOf(Passenger passenger)
    {
        if (passenger.Category is PassengerCategory.Senior or PassengerCategory.Child) return Priority;
        return passenger.UserId.HasValue ? App : NoAccount;
    }

    public static string Label(string segment) => segment switch
    {
        Priority => "Seniors and children",
        App => "App users",
        NoAccount => "No app account",
        _ => segment
    };

    public static string Describe(string segment) => segment switch
    {
        Priority => "Need boarding help. Told where staff will be.",
        App => "Receive the message in the commuter app.",
        NoAccount => "Cannot receive in-app messages. Staff must tell them at the terminal.",
        _ => ""
    };

    public static string Subject(string routeNumber) => $"Service update for route {routeNumber}";

    public static string DefaultMessage(string segment, string routeNumber, DateTime scheduledTime, string bayCode)
    {
        var departure = $"{scheduledTime:yyyy-MM-dd HH:mm} UTC";
        return segment switch
        {
            Priority => $"Your route {routeNumber} booking now departs at {departure} from bay {bayCode}. Staff at the bay will help you board. Your existing ticket remains valid.",
            NoAccount => $"Route {routeNumber} now departs at {departure} from bay {bayCode}. Please check with terminal staff at the bay. Your existing ticket remains valid.",
            _ => $"Your route {routeNumber} booking now departs at {departure} from bay {bayCode}. Your existing ticket remains valid."
        };
    }
}
