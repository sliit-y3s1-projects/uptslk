using api.Common.Constants;
using api.DTO.Passengers;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.DispatchStaff)]
[Route("api/v1/passengers")]
public class PassengersController(IPassengerService passengers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PassengerListItemResponse>>> List([FromQuery] PassengerFilter filter, CancellationToken cancellationToken) =>
        Ok(await passengers.ListAsync(filter, cancellationToken));

    [HttpGet("{passengerId:guid}")]
    public async Task<ActionResult<PassengerResponse>> Get(Guid passengerId, CancellationToken cancellationToken) =>
        Ok(await passengers.GetAsync(passengerId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PassengerCreatedResponse>> Create(CreatePassengerRequest request, CancellationToken cancellationToken)
    {
        var created = await passengers.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { passengerId = created.Id }, created);
    }

    [HttpPut("{passengerId:guid}")]
    public async Task<IActionResult> Update(Guid passengerId, UpdatePassengerRequest request, CancellationToken cancellationToken)
    {
        await passengers.UpdateAsync(passengerId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{passengerId:guid}")]
    public async Task<IActionResult> Deactivate(Guid passengerId, CancellationToken cancellationToken)
    {
        await passengers.DeactivateAsync(passengerId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{passengerId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid passengerId, CancellationToken cancellationToken)
    {
        await passengers.RestoreAsync(passengerId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{passengerId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid passengerId, ResetPassengerPasswordRequest request, CancellationToken cancellationToken)
    {
        await passengers.ResetPasswordAsync(passengerId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{passengerId:guid}/wallet/top-ups")]
    public async Task<ActionResult<WalletTopUpResponse>> TopUp(Guid passengerId, TopUpWalletRequest request, CancellationToken cancellationToken)
    {
        var topUp = await passengers.TopUpWalletAsync(passengerId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { passengerId }, topUp);
    }
}
