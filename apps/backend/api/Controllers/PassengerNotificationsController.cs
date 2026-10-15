using api.Common.Constants;
using api.DTO.Notifications;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/passenger-notifications")]
[Authorize(Roles = AppRoles.Commuter)]
public sealed class PassengerNotificationsController(IPassengerNotificationQueryService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PassengerNotificationResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await notifications.ListAsync(cancellationToken));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await notifications.MarkReadAsync(notificationId, cancellationToken);
        return NoContent();
    }
}
