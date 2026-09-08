using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/drivers")]
public class DriversController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] DriverStatus? status, [FromQuery] string? search)
    {
        var query = db.Drivers.AsNoTracking().Include(driver => driver.Centre).AsQueryable();
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
            driver.UserId
        }).ToListAsync();

        return Ok(drivers);
    }

    [HttpGet("{driverId:guid}")]
    public async Task<IActionResult> Get(Guid driverId)
    {
        var driver = await db.Drivers.AsNoTracking().Include(item => item.Centre).SingleOrDefaultAsync(item => item.Id == driverId);
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
            driver.UserId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDriverRequest request)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });

        var licenseNumber = NormalizeLicense(request.LicenseNumber);
        if (await db.Drivers.AnyAsync(driver => driver.LicenseNumber == licenseNumber)) return Conflict(new { error = "A driver with this license number already exists." });

        var driver = new Driver
        {
            CentreId = request.CentreId,
            FullName = request.FullName.Trim(),
            PhoneNumber = CleanOptional(request.PhoneNumber),
            LicenseNumber = licenseNumber,
            Status = request.Status
        };

        db.Drivers.Add(driver);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { driverId = driver.Id }, new { driver.Id, driver.CentreId, driver.FullName, driver.LicenseNumber, driver.Status });
    }

    [HttpPut("{driverId:guid}")]
    public async Task<IActionResult> Update(Guid driverId, UpdateDriverRequest request)
    {
        var driver = await db.Drivers.FindAsync(driverId);
        if (driver is null) return NotFound();
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });

        driver.CentreId = request.CentreId;
        driver.FullName = request.FullName.Trim();
        driver.PhoneNumber = CleanOptional(request.PhoneNumber);
        driver.Status = request.Status;
        driver.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{driverId:guid}")]
    public async Task<IActionResult> Deactivate(Guid driverId)
    {
        var driver = await db.Drivers.FindAsync(driverId);
        if (driver is null) return NotFound();

        driver.Status = DriverStatus.Inactive;
        driver.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizeLicense(string value) => value.Trim().ToUpperInvariant();
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
