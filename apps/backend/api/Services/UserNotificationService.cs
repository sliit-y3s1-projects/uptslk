using api.DTO.Notifications;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class UserNotificationService(
    IUserNotificationRepository notifications,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IUserNotificationService
{
    private const int MaxListed = 50;

    public async Task<IReadOnlyList<NotificationResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var latest = await notifications.GetLatestForUserAsync(currentUser.RequireUserId(), MaxListed, cancellationToken);
        return latest.Select(NotificationResponse.From).ToList();
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetForUserAsync(notificationId, currentUser.RequireUserId(), cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
        notification.ReadAt ??= DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var unread = await notifications.GetUnreadForUserAsync(currentUser.RequireUserId(), cancellationToken);
        var readAt = DateTime.UtcNow;
        foreach (var notification in unread) notification.ReadAt = readAt;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
