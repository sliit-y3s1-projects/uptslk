using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace api.Controllers;

[ApiController]
[Route("api/v1/drivers")]
public class DriversController(AppDbContext db, UserManager<User> userManager) : ControllerBase
{
    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] DriverStatus? status, [FromQuery] string? search)
    {
        var query = db.Drivers.AsNoTracking().Include(driver => driver.Centre).Include(driver => driver.User).AsQueryable();
        if (centreId.HasValue) query = query.Where(driver => driver.CentreId == centreId.Value);
        if (status.HasValue) query = query.Where(driver => driver.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(driver => driver.FullName.ToLower().Contains(term) || driver.LicenseNumber.ToLower().Contains(term));
        }

        var drivers = await query.OrderBy(driver => driver.FullName).Select(driver => new
        {
            driver.Id,
            driver.CentreId,
            Centre = driver.Centre.Name,
            driver.FullName,
            driver.PhoneNumber,
            driver.LicenseNumber,
            driver.Status,
            driver.UserId,
            Email = driver.User == null ? null : driver.User.Email
        }).ToListAsync();

        return Ok(drivers);
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpGet("{driverId:guid}")]
    public async Task<IActionResult> Get(Guid driverId)
    {
        var driver = await db.Drivers.AsNoTracking().Include(item => item.Centre).Include(item => item.User).SingleOrDefaultAsync(item => item.Id == driverId);
        if (driver is null) return NotFound();

        return Ok(new
        {
            driver.Id,
            driver.CentreId,
            Centre = new { driver.Centre.Id, driver.Centre.Code, driver.Centre.Name },
            driver.FullName,
            driver.PhoneNumber,
            driver.LicenseNumber,
            driver.Status,
            driver.UserId,
            Email = driver.User == null ? null : driver.User.Email
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,CentreManager")]
    public async Task<IActionResult> Create(CreateDriverRequest request)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });
        if (!CanManageCentre(request.CentreId)) return Forbid();

        var licenseNumber = NormalizeLicense(request.LicenseNumber);
        if (await db.Drivers.AnyAsync(driver => driver.LicenseNumber == licenseNumber)) return Conflict(new { error = "A driver with this license number already exists." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null) return Conflict(new { error = "An account with this email already exists." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new User
        {
            UserName = email,
            Email = email,
            Name = request.FullName.Trim(),
            Role = UserRole.Driver,
            CentreId = request.CentreId,
            IsActive = request.Status == DriverStatus.Active
        };

        var identityResult = await userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
            return BadRequest(new { error = identityResult.Errors.Select(error => error.Description) });

        var driver = new Driver
        {
            CentreId = request.CentreId,
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            PhoneNumber = CleanOptional(request.PhoneNumber),
            LicenseNumber = licenseNumber,
            Status = request.Status
        };

        db.Drivers.Add(driver);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(Get), new { driverId = driver.Id }, new { driver.Id, driver.CentreId, driver.FullName, driver.LicenseNumber, driver.Status, driver.UserId, user.Email });
    }

    [HttpPut("{driverId:guid}")]
    [Authorize(Roles = "Admin,CentreManager")]
    public async Task<IActionResult> Update(Guid driverId, UpdateDriverRequest request)
    {
        var driver = await db.Drivers.FindAsync(driverId);
        if (driver is null) return NotFound();
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });
        if (!CanManageCentre(driver.CentreId) || !CanManageCentre(request.CentreId)) return Forbid();

        driver.CentreId = request.CentreId;
        driver.FullName = request.FullName.Trim();
        driver.PhoneNumber = CleanOptional(request.PhoneNumber);
        driver.Status = request.Status;
        driver.UpdatedAt = DateTime.UtcNow;
        if (driver.UserId.HasValue)
        {
            var user = await userManager.FindByIdAsync(driver.UserId.Value.ToString());
            if (user is not null)
            {
                user.Name = driver.FullName;
                user.CentreId = driver.CentreId;
                user.IsActive = driver.Status == DriverStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;
                var identityResult = await userManager.UpdateAsync(user);
                if (!identityResult.Succeeded)
                    return BadRequest(new { error = identityResult.Errors.Select(error => error.Description) });
            }
        }
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{driverId:guid}")]
    [Authorize(Roles = "Admin,CentreManager")]
    public async Task<IActionResult> Deactivate(Guid driverId)
    {
        var driver = await db.Drivers.FindAsync(driverId);
        if (driver is null) return NotFound();
        if (!CanManageCentre(driver.CentreId)) return Forbid();

        driver.Status = DriverStatus.Inactive;
        driver.UpdatedAt = DateTime.UtcNow;
        if (driver.UserId.HasValue)
        {
            var user = await userManager.FindByIdAsync(driver.UserId.Value.ToString());
            if (user is not null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
            }
        }
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Me()
    {
        var driver = await CurrentDriverQuery().SingleOrDefaultAsync();
        if (driver is null) return NotFound(new { error = "This account is not linked to a driver record. Contact your centre manager." });

        return Ok(new
        {
            driver.Id,
            driver.FullName,
            driver.PhoneNumber,
            driver.LicenseNumber,
            driver.Status,
            driver.CentreId,
            CentreCode = driver.Centre.Code,
            CentreName = driver.Centre.Name,
            Email = driver.User!.Email
        });
    }

    [HttpGet("me/trips")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> MyTrips([FromQuery] DateOnly? date, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var driver = await CurrentDriverQuery().SingleOrDefaultAsync();
        if (driver is null) return NotFound(new { error = "This account is not linked to a driver record. Contact your centre manager." });

        var serviceDate = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Colombo"));
        if (fromDate.HasValue != toDate.HasValue)
            return BadRequest(new { error = "Provide both fromDate and toDate." });
        var firstDate = fromDate ?? serviceDate;
        var lastDate = toDate ?? serviceDate;
        if (lastDate.DayNumber < firstDate.DayNumber || lastDate.DayNumber - firstDate.DayNumber > 6)
            return BadRequest(new { error = "Duty searches can cover up to seven days." });
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
        var start = TimeZoneInfo.ConvertTimeToUtc(firstDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        var end = TimeZoneInfo.ConvertTimeToUtc(lastDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);

        var trips = await db.Trips.AsNoTracking()
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(trip => trip.Vehicle)
            .Include(trip => trip.Bay)
            .Include(trip => trip.Bookings)
            .Where(trip => trip.DriverId == driver.Id && trip.ScheduledTime >= start && trip.ScheduledTime < end)
            .OrderBy(trip => trip.ScheduledTime)
            .Select(trip => new
            {
                trip.Id,
                trip.RouteId,
                trip.RouteDirectionId,
                RouteNumber = trip.Route.RouteNumber,
                RouteName = trip.Route.Name,
                Origin = trip.RouteDirection == null ? trip.Route.Origin : trip.RouteDirection.StartCentre.Name,
                Destination = trip.RouteDirection == null ? trip.Route.Destination : trip.RouteDirection.EndCentre.Name,
                trip.ScheduledTime,
                trip.Status,
                VehiclePlate = trip.Vehicle.PlateNumber,
                VehicleModel = trip.Vehicle.Model,
                Capacity = trip.Vehicle.Capacity,
                BayCode = trip.Bay.Code,
                BayName = trip.Bay.Name,
                PassengerCount = trip.Bookings.Where(booking => booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed).Sum(booking => booking.PassengerCount),
                trip.Notes
            })
            .ToListAsync();

        return Ok(trips);
    }

    [HttpPatch("me/trips/{tripId:guid}/status")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> UpdateMyTripStatus(Guid tripId, UpdateTripStatusRequest request)
    {
        var driver = await CurrentDriverQuery().SingleOrDefaultAsync();
        if (driver is null) return NotFound(new { error = "This account is not linked to a driver record. Contact your centre manager." });

        var trip = await db.Trips.SingleOrDefaultAsync(item => item.Id == tripId && item.DriverId == driver.Id);
        if (trip is null) return NotFound(new { error = "The assigned trip was not found." });
        if (!CanDriverTransition(trip.Status, request.Status))
            return BadRequest(new { error = $"Cannot change a {trip.Status} trip to {request.Status}." });
        if (request.Status == TripStatus.Delayed && string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new { error = "Give a reason when marking a trip delayed." });

        trip.Status = request.Status;
        trip.Notes = CleanOptional(request.Note) ?? trip.Notes;
        trip.ActualDepartureAt = request.Status == TripStatus.Dispatched ? DateTime.UtcNow : trip.ActualDepartureAt;
        trip.CompletedAt = request.Status == TripStatus.Completed ? DateTime.UtcNow : trip.CompletedAt;
        trip.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("me/trips/{tripId:guid}/incidents")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> ReportIncident(Guid tripId, ReportDriverIncidentRequest request)
    {
        var driver = await CurrentDriverQuery().SingleOrDefaultAsync();
        if (driver is null) return NotFound(new { error = "This account is not linked to an active driver record." });
        var trip = await db.Trips.AsNoTracking().SingleOrDefaultAsync(item => item.Id == tripId && item.DriverId == driver.Id);
        if (trip is null) return NotFound(new { error = "The assigned trip was not found." });
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
            return BadRequest(new { error = "Incidents cannot be reported for completed or cancelled trips." });
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.Severity))
            return BadRequest(new { error = "Choose a valid incident type and severity." });
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { error = "Enter an incident title and description." });

        var incident = new Incident
        {
            CentreId = trip.CentreId,
            TripId = trip.Id,
            ReportedById = driver.UserId,
            ReportedByName = driver.FullName,
            Type = request.Type,
            Severity = request.Severity,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            SlaDueAt = DateTime.UtcNow.AddHours(4)
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync();
        return Created($"/api/v1/incidents/{incident.Id}", new { incident.Id, incident.TripId, incident.Status });
    }

    private IQueryable<Driver> CurrentDriverQuery()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId)
            ? db.Drivers.AsNoTracking().Include(driver => driver.Centre).Include(driver => driver.User)
                .Where(driver => driver.UserId == userId && driver.Status == DriverStatus.Active && driver.User != null && driver.User.IsActive)
            : db.Drivers.AsNoTracking().Where(_ => false);
    }

    private bool CanManageCentre(Guid centreId)
    {
        if (User.IsInRole(UserRole.Admin.ToString())) return true;
        var claim = User.FindFirstValue("centre_id");
        return User.IsInRole(UserRole.CentreManager.ToString()) && Guid.TryParse(claim, out var assignedCentreId) && assignedCentreId == centreId;
    }

    private static bool CanDriverTransition(TripStatus from, TripStatus to) => (from, to) switch
    {
        (TripStatus.Scheduled, TripStatus.Ready or TripStatus.Delayed) => true,
        (TripStatus.Ready, TripStatus.Boarding or TripStatus.Delayed) => true,
        (TripStatus.Boarding, TripStatus.Dispatched or TripStatus.Delayed) => true,
        (TripStatus.Delayed, TripStatus.Ready or TripStatus.Boarding or TripStatus.Dispatched) => true,
        (TripStatus.Dispatched, TripStatus.Completed or TripStatus.Delayed) => true,
        _ => false
    };

    private static string NormalizeLicense(string value) => value.Trim().ToUpperInvariant();
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
