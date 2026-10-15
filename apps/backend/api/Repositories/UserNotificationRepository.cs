using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class UserNotificationRepository(AppDbContext db) : Repository<UserNotification>(db), IUserNotificationRepository
{
    public Task<List<UserNotification>> GetLatestForUserAsync(Guid userId, int count, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<UserNotification?> GetForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        Set.SingleOrDefaultAsync(notification => notification.Id == notificationId && notification.UserId == userId, cancellationToken);

    public Task<List<UserNotification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.Where(notification => notification.UserId == userId && notification.ReadAt == null).ToListAsync(cancellationToken);
}
