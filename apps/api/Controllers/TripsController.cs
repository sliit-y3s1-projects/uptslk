using api.Common.Constants;
using api.DTO.Trips;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/trips")]
public class TripsController(ITripService trips) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TripListItemResponse>>> List([FromQuery] TripFilter filter, CancellationToken cancellationToken) =>
        Ok(await trips.ListAsync(filter, cancellationToken));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<TripListItemResponse>>> History([FromQuery] TripHistoryFilter filter, CancellationToken cancellationToken) =>
        Ok(await trips.ListHistoryAsync(filter, cancellationToken));

    [HttpGet("{tripId:guid}")]
    public async Task<ActionResult<TripResponse>> Get(Guid tripId, CancellationToken cancellationToken) =>
        Ok(await trips.GetAsync(tripId, cancellationToken));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPost]
    public async Task<ActionResult<TripCreatedResponse>> Create(CreateTripRequest request, CancellationToken cancellationToken)
    {
        var created = await trips.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { tripId = created.Id }, created);
    }

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPut("{tripId:guid}")]
    public async Task<IActionResult> Update(Guid tripId, UpdateTripRequest request, CancellationToken cancellationToken)
    {
        await trips.UpdateAsync(tripId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPost("{tripId:guid}/reassign")]
    public async Task<IActionResult> Reassign(Guid tripId, ReassignTripRequest request, CancellationToken cancellationToken)
    {
        await trips.ReassignAsync(tripId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpPatch("{tripId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid tripId, UpdateTripStatusRequest request, CancellationToken cancellationToken)
    {
        await trips.UpdateStatusAsync(tripId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpDelete("{tripId:guid}")]
    public async Task<IActionResult> Cancel(Guid tripId, CancelTripRequest request, CancellationToken cancellationToken)
    {
        await trips.CancelAsync(tripId, request, cancellationToken);
        return NoContent();
    }
}
