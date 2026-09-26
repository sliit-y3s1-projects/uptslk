using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Supabase.Storage.Exceptions;

namespace api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
public class VehiclesController(
    AppDbContext db,
    IImageStorageService imageStorage,
    ILogger<VehiclesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] VehicleStatus? status, [FromQuery] string? search)
    {
        var query = db.Vehicles.AsNoTracking().Include(vehicle => vehicle.Centre).AsQueryable();
        if (centreId.HasValue) query = query.Where(vehicle => vehicle.CentreId == centreId.Value);
        if (status.HasValue) query = query.Where(vehicle => vehicle.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(vehicle => vehicle.PlateNumber.ToLower().Contains(term) || vehicle.Model.ToLower().Contains(term));
        }

        var vehicles = await query.OrderBy(vehicle => vehicle.PlateNumber).Select(vehicle => new
        {
            vehicle.Id,
            vehicle.CentreId,
            Centre = vehicle.Centre.Name,
            vehicle.PlateNumber,
            vehicle.Model,
            vehicle.Type,
            vehicle.Capacity,
            vehicle.IsAccessible,
            vehicle.Status,
            vehicle.ImageUrl,
            MaintenanceCount = vehicle.MaintenanceRecords.Count(record => record.Status != MaintenanceStatus.Completed && record.Status != MaintenanceStatus.Cancelled)
        }).ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("{vehicleId:guid}")]
    public async Task<IActionResult> Get(Guid vehicleId)
    {
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(item => item.Centre)
            .Include(item => item.MaintenanceRecords)
            .SingleOrDefaultAsync(item => item.Id == vehicleId);

        if (vehicle is null) return NotFound();

        return Ok(new
        {
            vehicle.Id,
            vehicle.CentreId,
            Centre = new { vehicle.Centre.Id, vehicle.Centre.Code, vehicle.Centre.Name },
            vehicle.PlateNumber,
            vehicle.Model,
            vehicle.Type,
            vehicle.Capacity,
            vehicle.IsAccessible,
            vehicle.Status,
            vehicle.ImageUrl,
            Maintenance = vehicle.MaintenanceRecords.OrderByDescending(record => record.ScheduledFor).Select(record => new { record.Id, record.Type, record.Description, record.Status, record.ScheduledFor, record.CompletedAt })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVehicleRequest request)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });

        var plateNumber = NormalizePlate(request.PlateNumber);
        if (await db.Vehicles.AnyAsync(vehicle => vehicle.PlateNumber == plateNumber)) return Conflict(new { error = "A vehicle with this plate number already exists." });

        var vehicle = new Vehicle
        {
            CentreId = request.CentreId,
            PlateNumber = plateNumber,
            Model = request.Model.Trim(),
            Type = request.Type,
            Capacity = request.Capacity,
            IsAccessible = request.IsAccessible,
            Status = request.Status
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { vehicleId = vehicle.Id }, new { vehicle.Id, vehicle.CentreId, vehicle.PlateNumber, vehicle.Model, vehicle.Status, vehicle.ImageUrl });
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> Update(Guid vehicleId, UpdateVehicleRequest request)
    {
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null) return NotFound();
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });

        var plateNumber = NormalizePlate(request.PlateNumber);
        if (await db.Vehicles.AnyAsync(item => item.Id != vehicleId && item.PlateNumber == plateNumber)) return Conflict(new { error = "A vehicle with this plate number already exists." });

        vehicle.CentreId = request.CentreId;
        vehicle.PlateNumber = plateNumber;
        vehicle.Model = request.Model.Trim();
        vehicle.Type = request.Type;
        vehicle.Capacity = request.Capacity;
        vehicle.IsAccessible = request.IsAccessible;
        vehicle.Status = request.Status;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{vehicleId:guid}")]
    public async Task<IActionResult> Deactivate(Guid vehicleId)
    {
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null) return NotFound();

        vehicle.Status = VehicleStatus.Inactive;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{vehicleId:guid}/image")]
    [Authorize(Roles = "Admin,CentreManager,FleetOfficer")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        Guid vehicleId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length is 0 or > 5 * 1024 * 1024)
            return BadRequest(new { error = "Vehicle images must be between 1 byte and 5 MB." });

        var vehicle = await db.Vehicles.SingleOrDefaultAsync(
            item => item.Id == vehicleId,
            cancellationToken);
        if (vehicle is null) return NotFound();

        if (!User.IsInRole(nameof(UserRole.Admin)))
        {
            var centreClaim = User.FindFirst("centre_id")?.Value;
            if (!Guid.TryParse(centreClaim, out var centreId) || centreId != vehicle.CentreId)
                return Forbid();
        }

        try
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            vehicle.ImageUrl = await imageStorage.UploadVehicleAsync(
                vehicle.Id,
                stream.ToArray(),
                file.ContentType,
                cancellationToken);
            vehicle.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { vehicle.ImageUrl });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(exception, "Vehicle image storage is not configured");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Image storage is not configured."
            });
        }
        catch (SupabaseStorageException exception)
        {
            logger.LogError(exception, "Failed to upload an image for vehicle {VehicleId}", vehicleId);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "The vehicle image could not be uploaded. Please try again."
            });
        }
    }

    private static string NormalizePlate(string value) => value.Trim().ToUpperInvariant();
}
