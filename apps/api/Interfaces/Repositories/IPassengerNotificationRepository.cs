using api.Entities;

namespace api.Interfaces.Repositories;

public interface IPassengerNotificationRepository : IRepository<PassengerNotification>
{
    Task<List<PassengerNotification>> GetLatestDeliveredForUserAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task<PassengerNotification?> GetDeliveredForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}
