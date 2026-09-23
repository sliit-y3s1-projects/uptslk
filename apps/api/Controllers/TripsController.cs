using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteModel = api.Models.Route;

namespace api.Controllers;

[ApiController]
[Route("api/v1/trips")]
public class TripsController(AppDbContext db, TripConflictService conflictService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] Guid? terminalId, [FromQuery] Guid? routeId, [FromQuery] Guid? directionId, [FromQuery] Guid? vehicleId, [FromQuery] Guid? driverId, [FromQuery] Guid? bayId, [FromQuery] TripStatus? status, [FromQuery] DateOnly? date)
    {
        var query = db.Trips.AsNoTracking()
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(trip => trip.Vehicle)
            .Include(trip => trip.Driver)
            .Include(trip => trip.Bay)
            .AsQueryable();

        if (centreId.HasValue) query = query.Where(trip => trip.CentreId == centreId.Value);
        if (terminalId.HasValue) query = query.Where(trip => trip.RouteDirection != null && (trip.RouteDirection.StartCentreId == terminalId.Value || trip.RouteDirection.EndCentreId == terminalId.Value));
        if (routeId.HasValue) query = query.Where(trip => trip.RouteId == routeId.Value);
        if (directionId.HasValue) query = query.Where(trip => trip.RouteDirectionId == directionId.Value);
        if (vehicleId.HasValue) query = query.Where(trip => trip.VehicleId == vehicleId.Value);
        if (driverId.HasValue) query = query.Where(trip => trip.DriverId == driverId.Value);
        if (bayId.HasValue) query = query.Where(trip => trip.BayId == bayId.Value);
        if (status.HasValue) query = query.Where(trip => trip.Status == status.Value);
        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(trip => trip.ScheduledTime >= start && trip.ScheduledTime < end);
        }

        var trips = await query.OrderBy(trip => trip.ScheduledTime).ToListAsync();
        return Ok(await ToListItems(trips));
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] Guid? centreId, [FromQuery] DateOnly? date)
    {
        var query = db.Trips.AsNoTracking()
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(trip => trip.Vehicle)
            .Include(trip => trip.Driver)
            .Include(trip => trip.Bay)
            .Where(trip => trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled);

        if (centreId.HasValue) query = query.Where(trip => trip.CentreId == centreId.Value);
        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(trip => trip.ScheduledTime >= start && trip.ScheduledTime < end);
        }

        var trips = await query.OrderByDescending(trip => trip.ScheduledTime).ToListAsync();
        return Ok(await ToListItems(trips));
    }

    [HttpGet("{tripId:guid}")]
    public async Task<IActionResult> Get(Guid tripId)
    {
        var trip = await LoadTrip(tripId, true);
        if (trip is null) return NotFound();
        return Ok(ToDetail(trip));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTripRequest request)
    {
        var validation = await ValidateAssignment(request.CentreId, request.RouteId, request.RouteDirectionId, request.VehicleId, request.DriverId, request.BayId, request.ScheduledTime);
        if (validation.Errors.Count > 0) return BadRequest(new { errors = validation.Errors });

        var trip = new Trip
        {
            CentreId = request.CentreId,
            RouteId = request.RouteId,
            RouteDirectionId = request.RouteDirectionId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            BayId = request.BayId,
            ScheduledTime = request.ScheduledTime,
            Notes = CleanOptional(request.Notes)
        };

        db.Trips.Add(trip);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { tripId = trip.Id }, new { trip.Id, trip.CentreId, trip.RouteId, trip.ScheduledTime, trip.Status });
    }

    [HttpPut("{tripId:guid}")]
    public async Task<IActionResult> Update(Guid tripId, UpdateTripRequest request)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled) return BadRequest(new { error = "Completed or cancelled trips cannot be edited." });

        var validation = await ValidateAssignment(request.CentreId, request.RouteId, request.RouteDirectionId, request.VehicleId, request.DriverId, request.BayId, request.ScheduledTime, tripId);
        if (validation.Errors.Count > 0) return BadRequest(new { errors = validation.Errors });

        trip.CentreId = request.CentreId;
        trip.RouteId = request.RouteId;
        trip.RouteDirectionId = request.RouteDirectionId;
        trip.VehicleId = request.VehicleId;
        trip.DriverId = request.DriverId;
        trip.BayId = request.BayId;
        trip.ScheduledTime = request.ScheduledTime;
        trip.Notes = CleanOptional(request.Notes);
        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{tripId:guid}/reassign")]
    public async Task<IActionResult> Reassign(Guid tripId, ReassignTripRequest request)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled) return BadRequest(new { error = "Completed or cancelled trips cannot be reassigned." });

        var validation = await ValidateAssignment(trip.CentreId, trip.RouteId, trip.RouteDirectionId, request.VehicleId, request.DriverId, request.BayId, request.ScheduledTime, tripId);
        if (validation.Errors.Count > 0) return BadRequest(new { errors = validation.Errors });

        trip.VehicleId = request.VehicleId;
        trip.DriverId = request.DriverId;
        trip.BayId = request.BayId;
        trip.ScheduledTime = request.ScheduledTime;
        trip.Notes = CleanOptional(request.Notes) ?? trip.Notes;
        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{tripId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid tripId, UpdateTripStatusRequest request)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();
        if (!CanTransition(trip.Status, request.Status)) return BadRequest(new { error = $"Cannot change a {trip.Status} trip to {request.Status}." });

        trip.Status = request.Status;
        trip.Notes = CleanOptional(request.Note) ?? trip.Notes;
        trip.ActualDepartureAt = request.Status == TripStatus.Dispatched ? DateTime.UtcNow : trip.ActualDepartureAt;
        trip.CompletedAt = request.Status == TripStatus.Completed ? DateTime.UtcNow : trip.CompletedAt;
        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{tripId:guid}")]
    public async Task<IActionResult> Cancel(Guid tripId, CancelTripRequest request)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled) return BadRequest(new { error = "Only active trips can be cancelled." });

        trip.Status = TripStatus.Cancelled;
        trip.CancellationReason = request.Reason.Trim();
        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<AssignmentValidation> ValidateAssignment(Guid centreId, Guid routeId, Guid? directionId, Guid vehicleId, Guid driverId, Guid bayId, DateTime scheduledTime, Guid? excludedTripId = null)
    {
        var errors = new List<string>();
        var centre = await db.Centres.AsNoTracking().SingleOrDefaultAsync(item => item.Id == centreId);
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == routeId);
        var direction = directionId.HasValue
            ? await db.RouteDirections.AsNoTracking().SingleOrDefaultAsync(item => item.Id == directionId.Value)
            : null;
        var vehicle = await db.Vehicles.AsNoTracking().SingleOrDefaultAsync(item => item.Id == vehicleId);
        var driver = await db.Drivers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == driverId);
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == bayId);

        if (centre is null) errors.Add("The selected centre does not exist.");
        else if (centre.Status != CentreStatus.Operating) errors.Add("Trips can only be scheduled at an operating centre.");
        if (route is null) errors.Add("The selected route does not exist.");
        else if (!route.IsActive) errors.Add("The selected route is inactive.");
        if (directionId.HasValue && direction is null) errors.Add("The selected direction does not exist.");
        else if (direction is not null && !direction.IsActive) errors.Add("The selected direction is inactive.");
        if (vehicle is null) errors.Add("The selected vehicle does not exist.");
        else if (vehicle.Status != VehicleStatus.Active) errors.Add("The selected vehicle is not available for dispatch.");
        if (driver is null) errors.Add("The selected driver does not exist.");
        else if (driver.Status != DriverStatus.Active) errors.Add("The selected driver is not available for dispatch.");
        if (bay is null) errors.Add("The selected bay does not exist.");
        else if (bay.Status != BayStatus.Available) errors.Add("The selected bay is not available for dispatch.");

        if (route is not null && route.CentreId != centreId) errors.Add("The route does not belong to the selected centre.");
        if (direction is not null && direction.RouteId != routeId) errors.Add("The selected direction does not belong to the selected route.");
        if (vehicle is not null && vehicle.CentreId != centreId) errors.Add("The vehicle does not belong to the selected centre.");
        if (driver is not null && driver.CentreId != centreId) errors.Add("The driver does not belong to the selected centre.");
        if (bay is not null && direction is not null && bay.CentreId != direction.StartCentreId) errors.Add("The bay must belong to the direction's departure centre.");
        else if (bay is not null && direction is null && bay.CentreId != centreId) errors.Add("The bay does not belong to the selected centre.");

        if (route is not null && vehicle is not null && driver is not null && bay is not null && errors.Count == 0)
        {
            errors.AddRange(await conflictService.FindConflicts(vehicleId, driverId, bayId, scheduledTime, direction?.EstimatedDurationMin ?? route.EstimatedDurationMin, excludedTripId));
        }

        return new AssignmentValidation(errors);
    }

    private Task<Trip?> LoadTrip(Guid tripId, bool noTracking) =>
        (noTracking ? db.Trips.AsNoTracking() : db.Trips)
            .Include(trip => trip.Centre)
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(trip => trip.Vehicle)
            .Include(trip => trip.Driver)
            .Include(trip => trip.Bay)
            .SingleOrDefaultAsync(trip => trip.Id == tripId);

    private static bool CanTransition(TripStatus from, TripStatus to) => (from, to) switch
    {
        (TripStatus.Scheduled, TripStatus.Ready or TripStatus.Cancelled) => true,
        (TripStatus.Ready, TripStatus.Boarding or TripStatus.Delayed or TripStatus.Cancelled) => true,
        (TripStatus.Boarding, TripStatus.Dispatched or TripStatus.Delayed or TripStatus.Cancelled) => true,
        (TripStatus.Delayed, TripStatus.Ready or TripStatus.Boarding or TripStatus.Dispatched or TripStatus.Cancelled) => true,
        (TripStatus.Dispatched, TripStatus.Delayed or TripStatus.Completed) => true,
        _ => false
    };

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<IEnumerable<object>> ToListItems(IReadOnlyCollection<Trip> trips)
    {
        var tripIds = trips.Select(trip => trip.Id).ToArray();
        var occupied = await db.Bookings.AsNoTracking()
            .Where(booking => tripIds.Contains(booking.TripId) && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .GroupBy(booking => booking.TripId)
            .Select(group => new { TripId = group.Key, Count = group.Sum(booking => booking.PassengerCount) })
            .ToDictionaryAsync(item => item.TripId, item => item.Count);
        return trips.Select(trip => ToListItem(trip, occupied.GetValueOrDefault(trip.Id)));
    }

    private static object ToListItem(Trip trip, int occupied = 0) => new
    {
        trip.Id,
        trip.CentreId,
        trip.RouteId,
        trip.RouteDirectionId,
        RouteNumber = trip.Route.RouteNumber,
        RouteName = trip.Route.Name,
        DirectionName = trip.RouteDirection == null ? null : trip.RouteDirection.Name,
        Origin = trip.RouteDirection == null ? trip.Route.Origin : trip.RouteDirection.StartCentre.Name,
        Destination = trip.RouteDirection == null ? trip.Route.Destination : trip.RouteDirection.EndCentre.Name,
        trip.VehicleId,
        Vehicle = trip.Vehicle.PlateNumber,
        trip.DriverId,
        Driver = trip.Driver.FullName,
        trip.BayId,
        Bay = trip.Bay.Code,
        trip.ScheduledTime,
        trip.Status,
        trip.Notes
        ,Capacity = trip.Vehicle.Capacity
        ,Occupied = occupied
        ,Available = Math.Max(0, trip.Vehicle.Capacity - occupied)
        ,IsFull = occupied >= trip.Vehicle.Capacity
    };

    private static object ToDetail(Trip trip) => new
    {
        trip.Id,
        trip.CentreId,
        trip.RouteId,
        trip.RouteDirectionId,
        RouteNumber = trip.Route.RouteNumber,
        RouteName = trip.Route.Name,
        DirectionName = trip.RouteDirection == null ? null : trip.RouteDirection.Name,
        trip.VehicleId,
        Vehicle = trip.Vehicle.PlateNumber,
        trip.DriverId,
        Driver = trip.Driver.FullName,
        trip.BayId,
        Bay = trip.Bay.Code,
        Centre = new { trip.Centre.Id, trip.Centre.Code, trip.Centre.Name },
        Route = new { trip.Route.Id, trip.Route.RouteNumber, trip.Route.Name, Origin = trip.RouteDirection == null ? trip.Route.Origin : trip.RouteDirection.StartCentre.Name, Destination = trip.RouteDirection == null ? trip.Route.Destination : trip.RouteDirection.EndCentre.Name, EstimatedDurationMin = trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin },
        VehicleDetails = new { trip.Vehicle.Id, trip.Vehicle.PlateNumber, trip.Vehicle.Model, trip.Vehicle.Capacity, trip.Vehicle.IsAccessible },
        DriverDetails = new { trip.Driver.Id, trip.Driver.FullName, trip.Driver.LicenseNumber },
        BayDetails = new { trip.Bay.Id, trip.Bay.Code, trip.Bay.Name },
        trip.ScheduledTime,
        trip.Status,
        trip.Notes,
        trip.CancellationReason,
        trip.ActualDepartureAt,
        trip.CompletedAt,
        trip.CreatedAt,
        trip.UpdatedAt
    };

    private sealed record AssignmentValidation(IReadOnlyList<string> Errors);
}
