using api.Data;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery;

public sealed record PassengerNotificationPreparationResult(
    int Created,
    int Delivered,
    int Failed);

public sealed class PassengerNotificationService(AppDbContext db)
{
    public async Task<PassengerNotificationPreparationResult> PrepareApprovedRecoveryNotificationsAsync(
        AgentWorkflow workflow,
        RecoveryProposal proposal,
        CancellationToken cancellationToken)
    {
        if (!workflow.ApprovalRequests.Any(request => request.Decision == ApprovalDecision.Approved))
            throw new InvalidOperationException("Passenger notifications require an approved recovery workflow.");

        var bayCode = await db.Bays.AsNoTracking()
            .Where(bay => bay.Id == proposal.BayId)
            .Select(bay => bay.Code)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(bayCode))
            throw new InvalidOperationException("Passenger notifications require a valid departure bay.");

        var bookings = await db.Bookings.AsNoTracking()
            .Include(booking => booking.Passenger)
            .Where(booking => booking.TripId == workflow.TripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
                && !db.PassengerNotifications.Any(notification =>
                    notification.WorkflowId == workflow.Id
                    && notification.BookingId == booking.Id
                    && notification.Channel == PassengerNotificationChannel.InApp))
            .OrderBy(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);

        var createdAt = DateTime.UtcNow;
        var notifications = bookings
            .Select(booking => RecoveryNotificationFactory.Create(
                workflow,
                booking,
                proposal.ScheduledTime,
                bayCode,
                createdAt))
            .ToArray();
        db.PassengerNotifications.AddRange(notifications);

        return new PassengerNotificationPreparationResult(
            notifications.Length,
            notifications.Count(notification => notification.Status == PassengerNotificationStatus.Delivered),
            notifications.Count(notification => notification.Status == PassengerNotificationStatus.Failed));
    }

}
