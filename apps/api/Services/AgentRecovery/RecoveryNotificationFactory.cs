using api.Enums;
using api.Models;

namespace api.Services.AgentRecovery;

public static class RecoveryNotificationFactory
{
    public static PassengerNotification Create(
        AgentWorkflow workflow,
        Booking booking,
        DateTime scheduledTime,
        string bayCode,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(booking);
        if (string.IsNullOrWhiteSpace(bayCode))
            throw new ArgumentException("A departure bay code is required.", nameof(bayCode));

        var hasLinkedAccount = booking.Passenger.UserId.HasValue;
        var routeNumber = workflow.Trip.Route.RouteNumber;
        return new PassengerNotification
        {
            WorkflowId = workflow.Id,
            BookingId = booking.Id,
            PassengerId = booking.PassengerId,
            Subject = $"Service update for route {routeNumber}",
            Message = $"Your route {routeNumber} booking now departs at {scheduledTime:yyyy-MM-dd HH:mm} UTC from bay {bayCode}. Your existing ticket remains valid.",
            Status = hasLinkedAccount
                ? PassengerNotificationStatus.Delivered
                : PassengerNotificationStatus.Failed,
            DeliveryAttemptCount = 1,
            DeliveredAt = hasLinkedAccount ? createdAt : null,
            DeliveryError = hasLinkedAccount ? null : "Passenger has no linked commuter account.",
            CreatedAt = createdAt
        };
    }
}
