using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteModel = api.Models.Route;
using RouteDirectionModel = api.Models.RouteDirection;

namespace api.Controllers;

[ApiController]
[Route("api/v1/routes")]
public class RoutesController(AppDbContext db, TripConflictService conflictService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] string? search, [FromQuery] bool? active)
    {
        var query = db.Routes.AsNoTracking()
            .Include(route => route.Centre)
            .Include(route => route.Directions).ThenInclude(direction => direction.StartCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.EndCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.Stops)
            .Include(route => route.Directions).ThenInclude(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay)
            .AsQueryable();

        if (centreId.HasValue) query = query.Where(route => route.CentreId == centreId.Value);
        if (active.HasValue) query = query.Where(route => route.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(route => route.RouteNumber.ToLower().Contains(term) || route.Name.ToLower().Contains(term) || route.Origin.ToLower().Contains(term) || route.Destination.ToLower().Contains(term));
        }

        var routes = await query.OrderBy(route => route.RouteNumber).ToListAsync();
        return Ok(routes.Select(ToSummary));
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
            EstimatedDurationMin = request.EstimatedDurationMin
        };

        if (!request.StartCentreId.HasValue)
            route.Stops = ToStops(request.Stops, routeId: route.Id);

        if (request.StartCentreId.HasValue || request.EndCentreId.HasValue)
        {
            if (!request.StartCentreId.HasValue || !request.EndCentreId.HasValue)
                return BadRequest(new { error = "Both direction endpoint centres are required." });
            if (request.StartCentreId == request.EndCentreId)
                return BadRequest(new { error = "A direction needs different start and end centres." });
            var start = await db.Centres.FindAsync(request.StartCentreId.Value);
            var end = await db.Centres.FindAsync(request.EndCentreId.Value);
            if (start is null || end is null) return BadRequest(new { error = "One or more direction centres do not exist." });
            route.Origin = start.Name;
            route.Destination = end.Name;
            var direction = new RouteDirectionModel
            {
                StartCentreId = start.Id,
                EndCentreId = end.Id,
                Name = $"{start.Name} → {end.Name}",
                DistanceKm = request.DistanceKm,
                EstimatedDurationMin = request.EstimatedDurationMin
            };
            direction.Stops = ToStops(request.Stops, direction, route.Id);
            route.Directions.Add(direction);
        }

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

    [HttpPost("{routeId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid routeId)
    {
        var route = await db.Routes.FindAsync(routeId);
        if (route is null) return NotFound();

        route.IsActive = true;
        route.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{routeId:guid}/directions")]
    public async Task<IActionResult> ListDirections(Guid routeId)
    {
        var directions = await db.RouteDirections.AsNoTracking()
            .Include(direction => direction.StartCentre).Include(direction => direction.EndCentre)
            .Include(direction => direction.Stops).Include(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay)
            .Where(direction => direction.RouteId == routeId).OrderBy(direction => direction.Name).ToListAsync();
        return Ok(directions.Select(ToDirection));
    }

    [HttpPost("{routeId:guid}/directions")]
    public async Task<IActionResult> CreateDirection(Guid routeId, CreateRouteDirectionRequest request)
    {
        var route = await db.Routes.FindAsync(routeId);
        if (route is null) return NotFound();
        if (request.StartCentreId == request.EndCentreId) return BadRequest(new { error = "A direction needs different start and end centres." });
        var endpointCount = await db.Centres.CountAsync(centre => centre.Id == request.StartCentreId || centre.Id == request.EndCentreId);
        if (endpointCount != 2) return BadRequest(new { error = "One or more direction centres do not exist." });
        var direction = new RouteDirectionModel { RouteId = routeId, StartCentreId = request.StartCentreId, EndCentreId = request.EndCentreId, Name = request.Name.Trim(), DistanceKm = request.DistanceKm, EstimatedDurationMin = request.EstimatedDurationMin };
        direction.Stops = ToStops(request.Stops, direction, routeId);
        db.RouteDirections.Add(direction);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(ListDirections), new { routeId }, ToDirection(await LoadDirection(direction.Id, true) ?? direction));
    }

    [HttpPut("directions/{directionId:guid}")]
    public async Task<IActionResult> UpdateDirection(Guid directionId, UpdateRouteDirectionRequest request)
    {
        var direction = await LoadDirection(directionId, false);
        if (direction is null) return NotFound();
        if (request.StartCentreId == request.EndCentreId) return BadRequest(new { error = "A direction needs different start and end centres." });
        var endpointCount = await db.Centres.CountAsync(centre => centre.Id == request.StartCentreId || centre.Id == request.EndCentreId);
        if (endpointCount != 2) return BadRequest(new { error = "One or more direction centres do not exist." });
        direction.StartCentreId = request.StartCentreId;
        direction.EndCentreId = request.EndCentreId;
        direction.Name = request.Name.Trim();
        direction.DistanceKm = request.DistanceKm;
        direction.EstimatedDurationMin = request.EstimatedDurationMin;
        direction.IsActive = request.IsActive;
        direction.UpdatedAt = DateTime.UtcNow;
        db.RouteStops.RemoveRange(direction.Stops);
        direction.Stops = ToStops(request.Stops, direction, direction.RouteId);
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
                schedule.RouteDirectionId,
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

        RouteDirectionModel? direction = null;
        if (request.RouteDirectionId.HasValue)
        {
            direction = await db.RouteDirections.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RouteDirectionId.Value && item.RouteId == routeId);
            if (direction is null) return BadRequest(new { error = "The selected direction does not belong to this route." });
            if (!direction.IsActive) return BadRequest(new { error = "The selected direction is inactive." });
        }
        var departureCentreId = direction?.StartCentreId ?? route.CentreId;
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.BayId && item.CentreId == departureCentreId);
        if (bay is null) return BadRequest(new { error = "The selected bay must belong to this direction's departure centre." });

        var schedule = new RouteSchedule
        {
            RouteId = routeId,
            RouteDirectionId = request.RouteDirectionId,
            BayId = request.BayId,
            FirstDeparture = request.FirstDeparture,
            LastDeparture = request.LastDeparture,
            HeadwayMinutes = request.HeadwayMinutes,
            OperatingDays = request.OperatingDays.Trim()
        };

        db.RouteSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(ListSchedules), new { routeId }, new { schedule.Id, schedule.RouteId, schedule.RouteDirectionId, schedule.BayId, BayCode = bay.Code, schedule.FirstDeparture, schedule.LastDeparture, schedule.HeadwayMinutes, schedule.OperatingDays, schedule.IsActive });
    }

    [HttpPut("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid scheduleId, UpdateRouteScheduleRequest request)
    {
        var schedule = await db.RouteSchedules.Include(item => item.Route).Include(item => item.RouteDirection).SingleOrDefaultAsync(item => item.Id == scheduleId);
        if (schedule is null) return NotFound();
        if (!IsValidRange(request.FirstDeparture, request.LastDeparture)) return BadRequest(new { error = "Last departure must be later than first departure." });

        var departureCentreId = schedule.RouteDirection?.StartCentreId ?? schedule.Route.CentreId;
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.BayId && item.CentreId == departureCentreId);
        if (bay is null) return BadRequest(new { error = "The selected bay must belong to this direction's departure centre." });

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

    [HttpPost("schedules/{scheduleId:guid}/generate-trips")]
    public async Task<IActionResult> GenerateTrips(Guid scheduleId, GenerateScheduleTripsRequest request)
    {
        var schedule = await db.RouteSchedules
            .Include(item => item.Route)
            .Include(item => item.Bay)
            .Include(item => item.RouteDirection)
            .SingleOrDefaultAsync(item => item.Id == scheduleId);
        if (schedule is null) return NotFound();
        if (!schedule.IsActive) return BadRequest(new { error = "Only active schedules can generate trips." });
        if (schedule.HeadwayMinutes <= 0) return BadRequest(new { error = "The schedule must have a positive departure interval." });
        if (!OperatesOn(schedule.OperatingDays, request.ServiceDate))
            return BadRequest(new { error = "This schedule does not operate on the selected date." });

        var vehicles = await db.Vehicles.AsNoTracking()
            .Where(item => item.CentreId == schedule.Route.CentreId && item.Status == VehicleStatus.Active)
            .OrderBy(item => item.PlateNumber)
            .ToListAsync();
        var drivers = await db.Drivers.AsNoTracking()
            .Where(item => item.CentreId == schedule.Route.CentreId && item.Status == DriverStatus.Active)
            .OrderBy(item => item.FullName)
            .ToListAsync();
        if (vehicles.Count == 0 || drivers.Count == 0)
            return BadRequest(new { error = "Add at least one active vehicle and driver before generating trips." });
        if (schedule.Bay.Status != BayStatus.Available)
            return BadRequest(new { error = "The schedule's bay is not available for dispatch." });

        var created = 0;
        var existing = 0;
        var conflicts = 0;
        var createdDepartures = new List<string>();
        var skippedDepartures = new List<object>();
        var sequence = 0;
        for (var departure = schedule.FirstDeparture; departure <= schedule.LastDeparture; departure = departure.AddMinutes(schedule.HeadwayMinutes))
        {
            var localDeparture = request.ServiceDate.ToDateTime(departure, DateTimeKind.Unspecified);
            var scheduledTime = TimeZoneInfo.ConvertTimeToUtc(localDeparture, SriLankaTimeZone);
            var alreadyExists = await db.Trips.AnyAsync(item => item.RouteId == schedule.RouteId && item.RouteDirectionId == schedule.RouteDirectionId && item.ScheduledTime == scheduledTime);
            if (alreadyExists)
            {
                existing++;
                skippedDepartures.Add(new { time = departure.ToString("HH:mm"), reason = "A trip is already scheduled at this time." });
                sequence++;
                continue;
            }

            Trip? trip = null;
            for (var vehicleOffset = 0; vehicleOffset < vehicles.Count && trip is null; vehicleOffset++)
            {
                var vehicle = vehicles[(sequence + vehicleOffset) % vehicles.Count];
                for (var driverOffset = 0; driverOffset < drivers.Count; driverOffset++)
                {
                    var driver = drivers[(sequence + driverOffset) % drivers.Count];
                    var duration = schedule.RouteDirection?.EstimatedDurationMin ?? schedule.Route.EstimatedDurationMin;
                    var errors = await conflictService.FindConflicts(vehicle.Id, driver.Id, schedule.BayId, scheduledTime, duration);
                    if (errors.Count > 0) continue;
                    trip = new Trip
                    {
                        CentreId = schedule.Route.CentreId,
                        RouteId = schedule.RouteId,
                        RouteDirectionId = schedule.RouteDirectionId,
                        VehicleId = vehicle.Id,
                        DriverId = driver.Id,
                        BayId = schedule.BayId,
                        ScheduledTime = scheduledTime,
                        Notes = "Generated from recurring timetable."
                    };
                    break;
                }
            }

            if (trip is null)
            {
                conflicts++;
                skippedDepartures.Add(new { time = departure.ToString("HH:mm"), reason = "No free bus, driver, and bay combination was available." });
            }
            else
            {
                db.Trips.Add(trip);
                await db.SaveChangesAsync();
                created++;
                createdDepartures.Add(departure.ToString("HH:mm"));
            }
            sequence++;
        }

        return Ok(new
        {
            planned = created + existing + conflicts,
            created,
            existing,
            conflicts,
            serviceDate = request.ServiceDate,
            createdDepartures,
            skippedDepartures
        });
    }

    private Task<RouteModel?> LoadRoute(Guid routeId, bool noTracking) =>
        (noTracking ? db.Routes.AsNoTracking() : db.Routes)
            .Include(route => route.Centre)
            .Include(route => route.Stops)
            .Include(route => route.Schedules).ThenInclude(schedule => schedule.Bay)
            .Include(route => route.Directions).ThenInclude(direction => direction.StartCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.EndCentre)
            .Include(route => route.Directions).ThenInclude(direction => direction.Stops)
            .Include(route => route.Directions).ThenInclude(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay)
            .SingleOrDefaultAsync(route => route.Id == routeId);

    private Task<RouteDirectionModel?> LoadDirection(Guid directionId, bool noTracking) =>
        (noTracking ? db.RouteDirections.AsNoTracking() : db.RouteDirections)
            .Include(direction => direction.StartCentre).Include(direction => direction.EndCentre)
            .Include(direction => direction.Stops).Include(direction => direction.Schedules).ThenInclude(schedule => schedule.Bay)
            .SingleOrDefaultAsync(direction => direction.Id == directionId);

    private static List<RouteStop> ToStops(IEnumerable<RouteStopInput> stops, RouteDirectionModel? direction = null, Guid? routeId = null) => stops.Select((stop, index) => new RouteStop
    {
        RouteId = routeId ?? direction?.RouteId ?? Guid.Empty,
        RouteDirection = direction,
        StopName = stop.StopName.Trim(),
        SequenceOrder = index + 1,
        Latitude = stop.Latitude,
        Longitude = stop.Longitude
    }).ToList();

    private static bool IsValidRange(TimeOnly first, TimeOnly last) => last > first;

    private static readonly TimeZoneInfo SriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    private static bool OperatesOn(string operatingDays, DateOnly date)
    {
        if (operatingDays.Equals("Everyday", StringComparison.OrdinalIgnoreCase)) return true;
        if (operatingDays.Equals("Weekdays", StringComparison.OrdinalIgnoreCase)) return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
        if (operatingDays.Equals("Weekends", StringComparison.OrdinalIgnoreCase)) return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var day = date.DayOfWeek.ToString()[..3];
        return operatingDays.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(item => item.Equals(day, StringComparison.OrdinalIgnoreCase));
    }

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
        Schedules = route.Schedules.OrderBy(schedule => schedule.FirstDeparture).Select(schedule => new { schedule.Id, schedule.RouteDirectionId, schedule.BayId, BayCode = schedule.Bay.Code, schedule.FirstDeparture, schedule.LastDeparture, schedule.HeadwayMinutes, schedule.OperatingDays, schedule.IsActive }),
        Directions = route.Directions.Select(ToDirection)
    };

    private static object ToSummary(RouteModel route) => new
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
        StopCount = route.Directions.Sum(direction => direction.Stops.Count),
        ActiveScheduleCount = route.Directions.Sum(direction => direction.Schedules.Count(schedule => schedule.IsActive)),
        Directions = route.Directions.Select(ToDirection)
    };

    private static object ToDirection(RouteDirectionModel direction) => new
    {
        direction.Id,
        direction.RouteId,
        direction.StartCentreId,
        StartCentre = new { direction.StartCentre.Id, direction.StartCentre.Code, direction.StartCentre.Name },
        direction.EndCentreId,
        EndCentre = new { direction.EndCentre.Id, direction.EndCentre.Code, direction.EndCentre.Name },
        direction.Name,
        direction.DistanceKm,
        direction.EstimatedDurationMin,
        direction.IsActive,
        Stops = direction.Stops.OrderBy(stop => stop.SequenceOrder).Select(stop => new { stop.Id, stop.StopName, stop.SequenceOrder, stop.Latitude, stop.Longitude }),
        Schedules = direction.Schedules.OrderBy(schedule => schedule.FirstDeparture).Select(schedule => new { schedule.Id, schedule.RouteDirectionId, schedule.BayId, BayCode = schedule.Bay.Code, schedule.FirstDeparture, schedule.LastDeparture, schedule.HeadwayMinutes, schedule.OperatingDays, schedule.IsActive })
    };
}
