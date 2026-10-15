using api.DTO.Notifications;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class PassengerNotificationQueryService(
    IPassengerNotificationRepository notifications,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IPassengerNotificationQueryService
{
    private const int MaxListed = 50;

    public async Task<IReadOnlyList<PassengerNotificationResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var latest = await notifications.GetLatestDeliveredForUserAsync(currentUser.RequireUserId(), MaxListed, cancellationToken);
        return latest.Select(PassengerNotificationResponse.From).ToList();
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetDeliveredForUserAsync(notificationId, currentUser.RequireUserId(), cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
        notification.ReadAt ??= DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
