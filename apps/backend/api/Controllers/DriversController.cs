using api.Common.Constants;
using api.DTO.Drivers;
using api.DTO.Incidents;
using api.DTO.Trips;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/drivers")]
public class DriversController(IDriverService drivers, IDriverDutyService duties) : ControllerBase
{
    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverListItemResponse>>> List([FromQuery] DriverFilter filter, CancellationToken cancellationToken) =>
        Ok(await drivers.ListAsync(filter, cancellationToken));

    [Authorize(Roles = AppRoles.DispatchStaff)]
    [HttpGet("{driverId:guid}")]
    public async Task<ActionResult<DriverResponse>> Get(Guid driverId, CancellationToken cancellationToken) =>
        Ok(await drivers.GetAsync(driverId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<DriverCreatedResponse>> Create(CreateDriverRequest request, CancellationToken cancellationToken)
    {
        var created = await drivers.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { driverId = created.Id }, created);
    }

    [HttpPut("{driverId:guid}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Update(Guid driverId, UpdateDriverRequest request, CancellationToken cancellationToken)
    {
        await drivers.UpdateAsync(driverId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{driverId:guid}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Deactivate(Guid driverId, CancellationToken cancellationToken)
    {
        await drivers.DeactivateAsync(driverId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Driver)]
    public async Task<ActionResult<DriverProfileResponse>> Me(CancellationToken cancellationToken) =>
        Ok(await duties.GetProfileAsync(cancellationToken));

    [HttpGet("me/trips")]
    [Authorize(Roles = AppRoles.Driver)]
    public async Task<ActionResult<IReadOnlyList<DriverTripResponse>>> MyTrips([FromQuery] DriverTripFilter filter, CancellationToken cancellationToken) =>
        Ok(await duties.ListTripsAsync(filter, cancellationToken));

    [HttpPatch("me/trips/{tripId:guid}/status")]
    [Authorize(Roles = AppRoles.Driver)]
    public async Task<IActionResult> UpdateMyTripStatus(Guid tripId, UpdateTripStatusRequest request, CancellationToken cancellationToken)
    {
        await duties.UpdateTripStatusAsync(tripId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/trips/{tripId:guid}/incidents")]
    [Authorize(Roles = AppRoles.Driver)]
    public async Task<ActionResult<DriverIncidentCreatedResponse>> ReportIncident(Guid tripId, ReportDriverIncidentRequest request, CancellationToken cancellationToken)
    {
        var created = await duties.ReportIncidentAsync(tripId, request, cancellationToken);
        return Created($"/api/v1/incidents/{created.Id}", created);
    }
}
