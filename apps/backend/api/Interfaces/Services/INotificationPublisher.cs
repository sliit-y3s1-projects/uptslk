using api.DTO.Notifications;

namespace api.Interfaces.Services;

public interface INotificationPublisher
{
    Task PublishAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken);
}
