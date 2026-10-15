using api.Common.Constants;
using api.DTO.Notifications;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize(Roles = AppRoles.CommuterOrDriver)]
public sealed class NotificationsController(IUserNotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await notifications.ListAsync(cancellationToken));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await notifications.MarkReadAsync(notificationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notifications.MarkAllReadAsync(cancellationToken);
        return NoContent();
    }
}
