using api.Data;
using api.DTOs;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteModel = api.Models.Route;

namespace api.Controllers;

[ApiController]
[Route("api/v1/routes")]
public class RoutesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] string? search, [FromQuery] bool? active)
    {
        var query = db.Routes.AsNoTracking().Include(route => route.Centre).AsQueryable();

        if (centreId.HasValue) query = query.Where(route => route.CentreId == centreId.Value);
        if (active.HasValue) query = query.Where(route => route.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(route => route.RouteNumber.ToLower().Contains(term) || route.Name.ToLower().Contains(term) || route.Origin.ToLower().Contains(term) || route.Destination.ToLower().Contains(term));
        }

        var routes = await query.OrderBy(route => route.RouteNumber).Select(route => new
        {
            route.Id,
            route.CentreId,
            Centre = route.Centre.Name,
            route.RouteNumber,
            route.Name,
            route.Origin,
            route.Destination,
            route.ServiceType,
            route.DistanceKm,
            route.EstimatedDurationMin,
            route.IsActive,
            StopCount = route.Stops.Count,
            ActiveScheduleCount = route.Schedules.Count(schedule => schedule.IsActive)
        }).ToListAsync();

        return Ok(routes);
    }

    [HttpGet("{routeId:guid}")]
    public async Task<IActionResult> Get(Guid routeId)
    {
        var route = await LoadRoute(routeId, true);
        if (route is null) return NotFound();
        return Ok(ToDetail(route));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRouteRequest request)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });

        var routeNumber = request.RouteNumber.Trim().ToUpperInvariant();
        if (await db.Routes.AnyAsync(route => route.CentreId == request.CentreId && route.RouteNumber == routeNumber)) return Conflict(new { error = "This route number already exists at the selected centre." });

        var route = new RouteModel
        {
            CentreId = request.CentreId,
            RouteNumber = routeNumber,
            Name = request.Name.Trim(),
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            ServiceType = request.ServiceType,
            DistanceKm = request.DistanceKm,
            EstimatedDurationMin = request.EstimatedDurationMin,
            Stops = ToStops(request.Stops)
        };

        db.Routes.Add(route);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { routeId = route.Id }, new { route.Id, route.CentreId, route.RouteNumber, route.Name });
    }

    [HttpPut("{routeId:guid}")]
    public async Task<IActionResult> Update(Guid routeId, UpdateRouteRequest request)
    {
        var route = await LoadRoute(routeId, false);
        if (route is null) return NotFound();

        route.Name = request.Name.Trim();
        route.Origin = request.Origin.Trim();
        route.Destination = request.Destination.Trim();
        route.ServiceType = request.ServiceType;
        route.DistanceKm = request.DistanceKm;
        route.EstimatedDurationMin = request.EstimatedDurationMin;
        route.IsActive = request.IsActive;
        route.UpdatedAt = DateTime.UtcNow;

        db.RouteStops.RemoveRange(route.Stops);
        route.Stops = ToStops(request.Stops);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{routeId:guid}")]
    public async Task<IActionResult> Archive(Guid routeId)
    {
        var route = await db.Routes.FindAsync(routeId);
        if (route is null) return NotFound();

        route.IsActive = false;
        route.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{routeId:guid}/schedules")]
    public async Task<IActionResult> ListSchedules(Guid routeId)
    {
        if (!await db.Routes.AnyAsync(route => route.Id == routeId)) return NotFound();

        var schedules = await db.RouteSchedules.AsNoTracking().Include(schedule => schedule.Bay)
            .Where(schedule => schedule.RouteId == routeId)
            .OrderBy(schedule => schedule.FirstDeparture)
            .Select(schedule => new
            {
                schedule.Id,
                schedule.RouteId,
                schedule.BayId,
                BayCode = schedule.Bay.Code,
                schedule.FirstDeparture,
                schedule.LastDeparture,
                schedule.HeadwayMinutes,
                schedule.OperatingDays,
                schedule.IsActive
            }).ToListAsync();

        return Ok(schedules);
    }

    [HttpPost("{routeId:guid}/schedules")]
    public async Task<IActionResult> CreateSchedule(Guid routeId, CreateRouteScheduleRequest request)
    {
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == routeId);
        if (route is null) return NotFound();
        if (!IsValidRange(request.FirstDeparture, request.LastDeparture)) return BadRequest(new { error = "Last departure must be later than first departure." });

        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.BayId && item.CentreId == route.CentreId);
        if (bay is null) return BadRequest(new { error = "The selected bay does not belong to this route's centre." });

        var schedule = new RouteSchedule
        {
            RouteId = routeId,
            BayId = request.BayId,
            FirstDeparture = request.FirstDeparture,
            LastDeparture = request.LastDeparture,
            HeadwayMinutes = request.HeadwayMinutes,
            OperatingDays = request.OperatingDays.Trim()
        };

        db.RouteSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(ListSchedules), new { routeId }, new { schedule.Id, schedule.RouteId, schedule.BayId, schedule.FirstDeparture, schedule.LastDeparture, schedule.HeadwayMinutes, schedule.OperatingDays, schedule.IsActive });
    }

    [HttpPut("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid scheduleId, UpdateRouteScheduleRequest request)
    {
        var schedule = await db.RouteSchedules.Include(item => item.Route).SingleOrDefaultAsync(item => item.Id == scheduleId);
        if (schedule is null) return NotFound();
        if (!IsValidRange(request.FirstDeparture, request.LastDeparture)) return BadRequest(new { error = "Last departure must be later than first departure." });

        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.BayId && item.CentreId == schedule.Route.CentreId);
        if (bay is null) return BadRequest(new { error = "The selected bay does not belong to this route's centre." });

        schedule.BayId = request.BayId;
        schedule.FirstDeparture = request.FirstDeparture;
        schedule.LastDeparture = request.LastDeparture;
        schedule.HeadwayMinutes = request.HeadwayMinutes;
        schedule.OperatingDays = request.OperatingDays.Trim();
        schedule.IsActive = request.IsActive;
        schedule.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeactivateSchedule(Guid scheduleId)
    {
        var schedule = await db.RouteSchedules.FindAsync(scheduleId);
        if (schedule is null) return NotFound();

        schedule.IsActive = false;
        schedule.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private Task<RouteModel?> LoadRoute(Guid routeId, bool noTracking) =>
        (noTracking ? db.Routes.AsNoTracking() : db.Routes)
            .Include(route => route.Centre)
            .Include(route => route.Stops)
            .Include(route => route.Schedules).ThenInclude(schedule => schedule.Bay)
            .SingleOrDefaultAsync(route => route.Id == routeId);

    private static List<RouteStop> ToStops(IEnumerable<RouteStopInput> stops) => stops.Select((stop, index) => new RouteStop
    {
        StopName = stop.StopName.Trim(),
        SequenceOrder = index + 1,
        Latitude = stop.Latitude,
        Longitude = stop.Longitude
    }).ToList();

    private static bool IsValidRange(TimeOnly first, TimeOnly last) => last > first;

    private static object ToDetail(RouteModel route) => new
    {
        route.Id,
        route.CentreId,
        Centre = new { route.Centre.Id, route.Centre.Code, route.Centre.Name },
        route.RouteNumber,
        route.Name,
        route.Origin,
        route.Destination,
        route.ServiceType,
        route.DistanceKm,
        route.EstimatedDurationMin,
        route.IsActive,
        Stops = route.Stops.OrderBy(stop => stop.SequenceOrder).Select(stop => new { stop.Id, stop.StopName, stop.SequenceOrder, stop.Latitude, stop.Longitude }),
        Schedules = route.Schedules.OrderBy(schedule => schedule.FirstDeparture).Select(schedule => new { schedule.Id, schedule.BayId, BayCode = schedule.Bay.Code, schedule.FirstDeparture, schedule.LastDeparture, schedule.HeadwayMinutes, schedule.OperatingDays, schedule.IsActive })
    };
}
