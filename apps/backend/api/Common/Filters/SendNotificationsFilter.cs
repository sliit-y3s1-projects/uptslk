using api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace api.Common.Filters;

/// <summary>Pushes the notifications created during a request once the action has completed successfully.</summary>
public sealed class SendNotificationsFilter(IMobileNotificationService notifications) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception is null && executed.Result is not IStatusCodeActionResult { StatusCode: >= 400 })
            await notifications.SendPendingAsync(CancellationToken.None);
    }
}
