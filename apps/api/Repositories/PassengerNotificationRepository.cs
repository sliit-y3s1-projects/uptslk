using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PassengerNotificationRepository(AppDbContext db) : Repository<PassengerNotification>(db), IPassengerNotificationRepository
{
    public Task<List<PassengerNotification>> GetLatestDeliveredForUserAsync(Guid userId, int count, CancellationToken cancellationToken = default) =>
        DeliveredForUser(Set.AsNoTracking(), userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<PassengerNotification?> GetDeliveredForUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        DeliveredForUser(Set, userId).SingleOrDefaultAsync(notification => notification.Id == notificationId, cancellationToken);

    private static IQueryable<PassengerNotification> DeliveredForUser(IQueryable<PassengerNotification> query, Guid userId) =>
        query.Where(notification => notification.Passenger.UserId == userId
            && notification.Status == PassengerNotificationStatus.Delivered);
}
