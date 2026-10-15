using api.DTOs;
using api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace api.Services.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken);
}

public sealed class SignalRNotificationPublisher(IHubContext<NotificationHub> hub) : INotificationPublisher
{
    public Task PublishAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken) =>
        hub.Clients.User(userId.ToString()).SendAsync(NotificationHub.NotificationEvent, notification, cancellationToken);
}
