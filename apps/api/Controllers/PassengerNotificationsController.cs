using System.Security.Claims;
using api.Data;
using api.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/passenger-notifications")]
[Authorize(Roles = "Commuter")]
public sealed class PassengerNotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notifications = await db.PassengerNotifications.AsNoTracking()
            .Where(notification => notification.Passenger.UserId == userId
                && notification.Status == PassengerNotificationStatus.Delivered)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(50)
            .Select(notification => new
            {
                notification.Id,
                notification.Subject,
                notification.Message,
                notification.Channel,
                notification.Status,
                notification.DeliveredAt,
                notification.ReadAt,
                notification.BookingId,
                notification.WorkflowId,
                notification.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var notification = await db.PassengerNotifications
            .SingleOrDefaultAsync(item => item.Id == notificationId
                && item.Passenger.UserId == userId
                && item.Status == PassengerNotificationStatus.Delivered, cancellationToken);
        if (notification is null) return NotFound();

        notification.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
