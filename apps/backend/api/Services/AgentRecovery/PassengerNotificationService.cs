using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;

namespace api.Services.AgentRecovery;

public sealed record PassengerNotificationPreparationResult(
    int Created,
    int Delivered,
    int Failed);

// Writes the disruption notices passengers see after a recovery plan is approved.
public sealed class PassengerNotificationService(
    IBayRepository bays,
    IBookingRepository bookings,
    IPassengerNotificationRepository passengerNotifications)
{
    public async Task<PassengerNotificationPreparationResult> PrepareApprovedRecoveryNotificationsAsync(
        AgentWorkflow workflow,
        RecoveryProposal proposal,
        CancellationToken cancellationToken)
    {
        if (!workflow.ApprovalRequests.Any(request => request.Decision == ApprovalDecision.Approved))
            throw new InvalidOperationException("Passenger notifications require an approved recovery workflow.");

        var bay = await bays.GetReadOnlyAsync(proposal.BayId, cancellationToken);
        if (string.IsNullOrWhiteSpace(bay?.Code))
            throw new InvalidOperationException("Passenger notifications require a valid departure bay.");

        // Skip bookings that were already told, so running this twice does not send duplicates.
        var affectedBookings = await bookings.GetActiveWithoutRecoveryNoticeAsync(workflow.TripId, workflow.Id, cancellationToken);

        var createdAt = DateTime.UtcNow;
        var notifications = affectedBookings
            .Select(booking => RecoveryNotificationFactory.Create(workflow, booking, proposal.ScheduledTime, bay.Code, createdAt))
            .ToArray();
        // Saved by the caller together with the rest of the approved recovery.
        passengerNotifications.AddRange(notifications);

        return new PassengerNotificationPreparationResult(
            notifications.Length,
            notifications.Count(notification => notification.Status == PassengerNotificationStatus.Delivered),
            notifications.Count(notification => notification.Status == PassengerNotificationStatus.Failed));
    }
}
