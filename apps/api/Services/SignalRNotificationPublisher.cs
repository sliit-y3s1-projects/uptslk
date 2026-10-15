using api.DTO.Notifications;
using api.Hubs;
using api.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace api.Services;

public sealed class SignalRNotificationPublisher(IHubContext<NotificationHub> hub) : INotificationPublisher
{
    public Task PublishAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken) =>
        hub.Clients.User(userId.ToString()).SendAsync(NotificationHub.NotificationEvent, notification, cancellationToken);
}
