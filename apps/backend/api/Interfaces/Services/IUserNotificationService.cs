using api.DTO.Notifications;

namespace api.Interfaces.Services;

// The signed-in user's in-app notifications.
public interface IUserNotificationService
{
    Task<IReadOnlyList<NotificationResponse>> ListAsync(CancellationToken cancellationToken);
    Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken);
    Task MarkAllReadAsync(CancellationToken cancellationToken);
}
