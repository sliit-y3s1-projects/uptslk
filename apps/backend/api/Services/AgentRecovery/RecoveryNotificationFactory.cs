using api.Entities.Enums;
using api.Entities;
using api.Services.AgentRecovery.Copilot;

namespace api.Services.AgentRecovery;

public static class RecoveryNotificationFactory
{
    public static PassengerNotification Create(
        AgentWorkflow workflow,
        Booking booking,
        DateTime scheduledTime,
        string bayCode,
        DateTime createdAt,
        IReadOnlyDictionary<string, NotificationDraft>? drafts = null)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(booking);
        if (string.IsNullOrWhiteSpace(bayCode))
            throw new ArgumentException("A departure bay code is required.", nameof(bayCode));

        var hasLinkedAccount = booking.Passenger.UserId.HasValue;
        var routeNumber = workflow.Trip.Route.RouteNumber;
        var segment = RecoveryMessageSegments.SegmentOf(booking);
        // Wording the coordinator edited for this group wins over the default.
        NotificationDraft? draft = null;
        drafts?.TryGetValue(segment, out draft);
        return new PassengerNotification
        {
            WorkflowId = workflow.Id,
            BookingId = booking.Id,
            PassengerId = booking.PassengerId,
            Subject = string.IsNullOrWhiteSpace(draft?.Subject) ? RecoveryMessageSegments.Subject(routeNumber) : draft.Subject,
            Message = draft?.Message ?? RecoveryMessageSegments.DefaultMessage(segment, routeNumber, scheduledTime, bayCode),
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
