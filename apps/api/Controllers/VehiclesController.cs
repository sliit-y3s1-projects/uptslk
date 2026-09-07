using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
public class VehiclesController(AppDbContext db) : ControllerBase
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
        return CreatedAtAction(nameof(Get), new { vehicleId = vehicle.Id }, new { vehicle.Id, vehicle.CentreId, vehicle.PlateNumber, vehicle.Model, vehicle.Status });
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

    private static string NormalizePlate(string value) => value.Trim().ToUpperInvariant();
}
