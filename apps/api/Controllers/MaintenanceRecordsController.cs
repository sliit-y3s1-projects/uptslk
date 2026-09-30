using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace api.Controllers;

[ApiController]
[Route("api/v1/maintenance-records")]
public class MaintenanceRecordsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? vehicleId, [FromQuery] Guid? centreId, [FromQuery] MaintenanceStatus? status)
    {
        var query = db.MaintenanceRecords.AsNoTracking().Include(record => record.Vehicle).ThenInclude(vehicle => vehicle.Centre).AsQueryable();
        if (vehicleId.HasValue) query = query.Where(record => record.VehicleId == vehicleId.Value);
        if (centreId.HasValue) query = query.Where(record => record.Vehicle.CentreId == centreId.Value);
        if (status.HasValue) query = query.Where(record => record.Status == status.Value);

        var records = await query.OrderBy(record => record.ScheduledFor).Select(record => new
        {
            record.Id,
            record.VehicleId,
            Vehicle = record.Vehicle.PlateNumber,
            record.Vehicle.CentreId,
            Centre = record.Vehicle.Centre.Name,
            record.Type,
            record.Description,
            record.Status,
            record.ScheduledFor,
            record.CompletedAt
        }).ToListAsync();

        return Ok(records);
    }

    [HttpGet("{recordId:guid}")]
    public async Task<IActionResult> Get(Guid recordId)
    {
        var record = await db.MaintenanceRecords.AsNoTracking().Include(item => item.Vehicle).ThenInclude(vehicle => vehicle.Centre).SingleOrDefaultAsync(item => item.Id == recordId);
        if (record is null) return NotFound();

        return Ok(new
        {
            record.Id,
            record.VehicleId,
            Vehicle = new { record.Vehicle.Id, record.Vehicle.PlateNumber, record.Vehicle.Model, record.Vehicle.CentreId },
            record.Type,
            record.Description,
            record.Status,
            record.ScheduledFor,
            record.CompletedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,CentreManager,FleetOfficer")]
    public async Task<IActionResult> Create(CreateMaintenanceRecordRequest request)
    {
        var vehicle = await db.Vehicles.FindAsync(request.VehicleId);
        if (vehicle is null) return BadRequest(new { error = "The selected vehicle does not exist." });
        if (!CanManageCentre(vehicle.CentreId)) return Forbid();

        var record = new MaintenanceRecord
        {
            VehicleId = request.VehicleId,
            Type = request.Type.Trim(),
            Description = request.Description.Trim(),
            ScheduledFor = request.ScheduledFor
        };

        var conflictingTrips = await VehicleMaintenanceRules.FindConflictingTripIdsAsync(db, record);
        if (conflictingTrips.Count > 0)
            return Conflict(new { error = "Reassign or cancel the vehicle's active trips on this maintenance day before scheduling it.", conflictingTripIds = conflictingTrips });

        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { recordId = record.Id }, new { record.Id, record.VehicleId, record.Type, record.Status, record.ScheduledFor });
    }

    [HttpPut("{recordId:guid}")]
    [Authorize(Roles = "Admin,CentreManager,FleetOfficer")]
    public async Task<IActionResult> Update(Guid recordId, UpdateMaintenanceRecordRequest request)
    {
        var record = await db.MaintenanceRecords.Include(item => item.Vehicle).SingleOrDefaultAsync(item => item.Id == recordId);
        if (record is null) return NotFound();
        if (!CanManageCentre(record.Vehicle.CentreId)) return Forbid();
        if (request.Status == MaintenanceStatus.Completed && request.CompletedAt is null) return BadRequest(new { error = "Completed maintenance requires a completion time." });

        var proposedRecord = new MaintenanceRecord
        {
            VehicleId = record.VehicleId,
            Status = request.Status,
            ScheduledFor = request.ScheduledFor
        };
        var conflictingTrips = await VehicleMaintenanceRules.FindConflictingTripIdsAsync(db, proposedRecord);
        if (conflictingTrips.Count > 0)
            return Conflict(new { error = "Reassign or cancel the vehicle's active trips before reserving it for maintenance.", conflictingTripIds = conflictingTrips });

        record.Type = request.Type.Trim();
        record.Description = request.Description.Trim();
        record.Status = request.Status;
        record.ScheduledFor = request.ScheduledFor;
        record.CompletedAt = request.CompletedAt;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{recordId:guid}")]
    [Authorize(Roles = "Admin,CentreManager,FleetOfficer")]
    public async Task<IActionResult> Cancel(Guid recordId)
    {
        var record = await db.MaintenanceRecords.Include(item => item.Vehicle).SingleOrDefaultAsync(item => item.Id == recordId);
        if (record is null) return NotFound();
        if (!CanManageCentre(record.Vehicle.CentreId)) return Forbid();

        record.Status = MaintenanceStatus.Cancelled;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private bool CanManageCentre(Guid centreId) =>
        User.IsInRole("Admin") || Guid.TryParse(User.FindFirstValue("centre_id"), out var assignedCentreId) && assignedCentreId == centreId;
}
