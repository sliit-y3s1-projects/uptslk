using api.Enums;
using api.Models;

namespace api.DTOs;

public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    Guid? TripId,
    Guid? BookingId,
    DateTime? ReadAt,
    DateTime CreatedAt)
{
    public static NotificationResponse From(UserNotification notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.Message,
        notification.TripId,
        notification.BookingId,
        notification.ReadAt,
        notification.CreatedAt);
}
