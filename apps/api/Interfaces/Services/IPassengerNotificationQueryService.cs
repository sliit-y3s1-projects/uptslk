using api.DTO.Notifications;

namespace api.Interfaces.Services;

// The signed-in commuter's disruption notices from the recovery workflow.
public interface IPassengerNotificationQueryService
{
    Task<IReadOnlyList<PassengerNotificationResponse>> ListAsync(CancellationToken cancellationToken);
    Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken);
}
