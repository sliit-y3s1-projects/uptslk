using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Notifications;

public sealed record PassengerNotificationResponse(
    Guid Id,
    string Subject,
    string Message,
    PassengerNotificationChannel Channel,
    PassengerNotificationStatus Status,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    Guid BookingId,
    Guid WorkflowId,
    DateTime CreatedAt)
{
    public static PassengerNotificationResponse From(PassengerNotification notification) => new(
        notification.Id,
        notification.Subject,
        notification.Message,
        notification.Channel,
        notification.Status,
        notification.DeliveredAt,
        notification.ReadAt,
        notification.BookingId,
        notification.WorkflowId,
        notification.CreatedAt);
}
