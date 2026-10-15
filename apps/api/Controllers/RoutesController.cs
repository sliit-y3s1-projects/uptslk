using api.Common.Constants;
using api.DTO.Routes;
using api.DTO.Trips;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/routes")]
public class RoutesController(IRouteService routes, IRouteScheduleService schedules) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RouteSummaryResponse>>> List([FromQuery] RouteFilter filter, CancellationToken cancellationToken) =>
        Ok(await routes.ListAsync(filter, cancellationToken));

    [HttpGet("{routeId:guid}")]
    public async Task<ActionResult<RouteResponse>> Get(Guid routeId, CancellationToken cancellationToken) =>
        Ok(await routes.GetAsync(routeId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost]
    public async Task<ActionResult<RouteCreatedResponse>> Create(CreateRouteRequest request, CancellationToken cancellationToken)
    {
        var created = await routes.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { routeId = created.Id }, created);
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPut("{routeId:guid}")]
    public async Task<IActionResult> Update(Guid routeId, UpdateRouteRequest request, CancellationToken cancellationToken)
    {
        await routes.UpdateAsync(routeId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpDelete("{routeId:guid}")]
    public async Task<IActionResult> Archive(Guid routeId, CancellationToken cancellationToken)
    {
        await routes.SetActiveAsync(routeId, false, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost("{routeId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid routeId, CancellationToken cancellationToken)
    {
        await routes.SetActiveAsync(routeId, true, cancellationToken);
        return NoContent();
    }

    [HttpGet("{routeId:guid}/directions")]
    public async Task<ActionResult<IReadOnlyList<RouteDirectionResponse>>> ListDirections(Guid routeId, CancellationToken cancellationToken) =>
        Ok(await routes.ListDirectionsAsync(routeId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost("{routeId:guid}/directions")]
    public async Task<ActionResult<RouteDirectionResponse>> CreateDirection(Guid routeId, CreateRouteDirectionRequest request, CancellationToken cancellationToken)
    {
        var created = await routes.CreateDirectionAsync(routeId, request, cancellationToken);
        return CreatedAtAction(nameof(ListDirections), new { routeId }, created);
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPut("directions/{directionId:guid}")]
    public async Task<IActionResult> UpdateDirection(Guid directionId, UpdateRouteDirectionRequest request, CancellationToken cancellationToken)
    {
        await routes.UpdateDirectionAsync(directionId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{routeId:guid}/schedules")]
    public async Task<ActionResult<IReadOnlyList<RouteScheduleResponse>>> ListSchedules(Guid routeId, CancellationToken cancellationToken) =>
        Ok(await schedules.ListAsync(routeId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost("{routeId:guid}/schedules")]
    public async Task<ActionResult<RouteScheduleResponse>> CreateSchedule(Guid routeId, CreateRouteScheduleRequest request, CancellationToken cancellationToken)
    {
        var created = await schedules.CreateAsync(routeId, request, cancellationToken);
        return CreatedAtAction(nameof(ListSchedules), new { routeId }, created);
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPut("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid scheduleId, UpdateRouteScheduleRequest request, CancellationToken cancellationToken)
    {
        await schedules.UpdateAsync(scheduleId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpDelete("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeactivateSchedule(Guid scheduleId, CancellationToken cancellationToken)
    {
        await schedules.DeactivateAsync(scheduleId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost("schedules/{scheduleId:guid}/generate-trips")]
    public async Task<ActionResult<GeneratedTripsResponse>> GenerateTrips(Guid scheduleId, GenerateScheduleTripsRequest request, CancellationToken cancellationToken) =>
        Ok(await schedules.GenerateTripsAsync(scheduleId, request, cancellationToken));
}
