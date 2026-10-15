using api.Entities;

namespace api.Interfaces.Repositories;

public interface IUserNotificationRepository : IRepository<UserNotification>
{
    Task<List<UserNotification>> GetLatestForUserAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task<UserNotification?> GetForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserNotification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
