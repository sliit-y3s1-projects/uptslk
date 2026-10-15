using System.Collections.Concurrent;
using api.Data;
using api.DTO.Notifications;
using api.Interfaces.Services;
using api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Tests.Shared;

/// <summary>Records SignalR pushes instead of sending them.</summary>
internal sealed class RecordingNotificationPublisher : INotificationPublisher
{
    public ConcurrentQueue<(Guid UserId, NotificationResponse Notification)> Sent { get; } = new();

    public Task PublishAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken)
    {
        Sent.Enqueue((userId, notification));
        return Task.CompletedTask;
    }

    public void Register(IServiceCollection services) => services.AddSingleton<INotificationPublisher>(this);

    public static MobileNotificationService ServiceFor(AppDbContext db) =>
        new(db, new RecordingNotificationPublisher(), NullLogger<MobileNotificationService>.Instance);
}
