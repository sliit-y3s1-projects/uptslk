using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleResponseDto>>> List(
        [FromQuery] Guid? centreId,
        [FromQuery] VehicleStatus? status,
        [FromQuery] bool? isActive)
    {
        var query = db.Vehicles.AsNoTracking().AsQueryable();

        if (centreId.HasValue) query = query.Where(v => v.CentreId == centreId.Value);
        if (status.HasValue) query = query.Where(v => v.Status == status.Value);
        if (isActive.HasValue) query = query.Where(v => v.IsActive == isActive.Value);

        var vehicles = await query
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VehicleResponseDto
            {
                VehicleId = v.VehicleId,
                RegistrationNumber = v.RegistrationNumber,
                VehicleType = v.VehicleType,
                Capacity = v.Capacity,
                Status = v.Status,
                CentreId = v.CentreId,
                CentreName = v.CentreName,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                Model = v.Model
            })
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("{id:guid}", Name = nameof(Get))]
    public async Task<ActionResult<VehicleResponseDto>> Get(Guid id)
    {
        var vehicle = await db.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle is null)
        {
            return NotFound(new { error = $"Vehicle with ID {id} not found." });
        }

        return Ok(new VehicleResponseDto
        {
            VehicleId = vehicle.VehicleId,
            RegistrationNumber = vehicle.RegistrationNumber,
            VehicleType = vehicle.VehicleType,
            Capacity = vehicle.Capacity,
            Status = vehicle.Status,
            CentreId = vehicle.CentreId,
            CentreName = vehicle.CentreName,
            IsActive = vehicle.IsActive,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
            Model = vehicle.Model
        });
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponseDto>> Create([FromBody] VehicleCreateDto request)
    {
        if (request.Capacity <= 0)
        {
            return BadRequest(new { error = "Capacity must be greater than 0." });
        }

        var registrationNumber = NormalizeRegistration(request.RegistrationNumber);
        if (await db.Vehicles.AnyAsync(v => v.RegistrationNumber.ToUpper() == registrationNumber))
        {
            return Conflict(new { error = "A vehicle with this registration number already exists." });
        }

        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            CentreId = request.CentreId,
            CentreName = request.CentreName?.Trim() ?? string.Empty,
            RegistrationNumber = registrationNumber,
            VehicleType = request.VehicleType,
            Capacity = request.Capacity,
            Status = request.Status,
            IsActive = true,
            Model = request.Model?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var response = new VehicleResponseDto
        {
            VehicleId = vehicle.VehicleId,
            RegistrationNumber = vehicle.RegistrationNumber,
            VehicleType = vehicle.VehicleType,
            Capacity = vehicle.Capacity,
            Status = vehicle.Status,
            CentreId = vehicle.CentreId,
            CentreName = vehicle.CentreName,
            IsActive = vehicle.IsActive,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
            Model = vehicle.Model
        };

        return CreatedAtAction(nameof(Get), new { id = vehicle.VehicleId }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleResponseDto>> Update(Guid id, [FromBody] VehicleUpdateDto request)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);
        if (vehicle is null)
        {
            return NotFound(new { error = $"Vehicle with ID {id} not found." });
        }

        if (request.Capacity <= 0)
        {
            return BadRequest(new { error = "Capacity must be greater than 0." });
        }

        var registrationNumber = NormalizeRegistration(request.RegistrationNumber);
        if (await db.Vehicles.AnyAsync(v => v.VehicleId != id && v.RegistrationNumber.ToUpper() == registrationNumber))
        {
            return Conflict(new { error = "A vehicle with this registration number already exists." });
        }

        vehicle.RegistrationNumber = registrationNumber;
        vehicle.VehicleType = request.VehicleType;
        vehicle.Capacity = request.Capacity;
        vehicle.Status = request.Status;
        vehicle.CentreId = request.CentreId;
        if (!string.IsNullOrWhiteSpace(request.CentreName)) vehicle.CentreName = request.CentreName.Trim();
        if (request.Model is not null) vehicle.Model = request.Model.Trim();
        if (request.IsActive.HasValue) vehicle.IsActive = request.IsActive.Value;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new VehicleResponseDto
        {
            VehicleId = vehicle.VehicleId,
            RegistrationNumber = vehicle.RegistrationNumber,
            VehicleType = vehicle.VehicleType,
            Capacity = vehicle.Capacity,
            Status = vehicle.Status,
            CentreId = vehicle.CentreId,
            CentreName = vehicle.CentreName,
            IsActive = vehicle.IsActive,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
            Model = vehicle.Model
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id);
        if (vehicle is null)
        {
            return NotFound(new { error = $"Vehicle with ID {id} not found." });
        }

        // Soft-delete -> sets IsActive = false
        vehicle.IsActive = false;
        vehicle.Status = VehicleStatus.Inactive;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizeRegistration(string value) => value.Trim().ToUpperInvariant();
}
