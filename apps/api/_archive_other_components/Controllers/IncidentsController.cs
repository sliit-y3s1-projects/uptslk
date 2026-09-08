using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/incidents")]
public class IncidentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] Guid? tripId, [FromQuery] IncidentStatus? status, [FromQuery] IncidentType? type, [FromQuery] IncidentSeverity? severity)
    {
        var query = db.Incidents.AsNoTracking().Include(incident => incident.Trip).AsQueryable();
        if (centreId.HasValue) query = query.Where(incident => incident.CentreId == centreId.Value);
        if (tripId.HasValue) query = query.Where(incident => incident.TripId == tripId.Value);
        if (status.HasValue) query = query.Where(incident => incident.Status == status.Value);
        if (type.HasValue) query = query.Where(incident => incident.Type == type.Value);
        if (severity.HasValue) query = query.Where(incident => incident.Severity == severity.Value);

        var incidents = await query.OrderByDescending(incident => incident.CreatedAt).Select(incident => new
        {
            incident.Id,
            incident.CentreId,
            incident.TripId,
            TripTime = incident.Trip != null ? incident.Trip.ScheduledTime : (DateTime?)null,
            incident.ReportedByName,
            incident.Type,
            incident.Severity,
            incident.Title,
            incident.AssignedTo,
            incident.Status,
            incident.SlaDueAt,
            incident.ResolvedAt,
            incident.CreatedAt
        }).ToListAsync();

        return Ok(incidents);
    }

    [HttpGet("{incidentId:guid}")]
    public async Task<IActionResult> Get(Guid incidentId)
    {
        var incident = await db.Incidents.AsNoTracking().Include(item => item.Centre).Include(item => item.Trip).SingleOrDefaultAsync(item => item.Id == incidentId);
        if (incident is null) return NotFound();

        return Ok(new
        {
            incident.Id,
            Centre = new { incident.Centre.Id, incident.Centre.Code, incident.Centre.Name },
            incident.TripId,
            Trip = incident.Trip is null ? null : new { incident.Trip.Id, incident.Trip.ScheduledTime, incident.Trip.Status },
            incident.ReportedByName,
            incident.Type,
            incident.Severity,
            incident.Title,
            incident.Description,
            incident.AssignedTo,
            incident.Status,
            incident.SlaDueAt,
            incident.ResolvedAt,
            incident.CreatedAt,
            incident.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateIncidentRequest request)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == request.CentreId)) return BadRequest(new { error = "The selected centre does not exist." });
        if (request.TripId.HasValue && !await db.Trips.AnyAsync(trip => trip.Id == request.TripId.Value && trip.CentreId == request.CentreId)) return BadRequest(new { error = "The selected trip does not belong to this centre." });

        var incident = new Incident
        {
            CentreId = request.CentreId,
            TripId = request.TripId,
            ReportedByName = request.ReportedByName.Trim(),
            Type = request.Type,
            Severity = request.Severity,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AssignedTo = CleanOptional(request.AssignedTo),
            SlaDueAt = request.SlaDueAt ?? DateTime.UtcNow.AddHours(4)
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { incidentId = incident.Id }, new { incident.Id, incident.CentreId, incident.TripId, incident.Title, incident.Status });
    }

    [HttpPut("{incidentId:guid}")]
    public async Task<IActionResult> Update(Guid incidentId, UpdateIncidentRequest request)
    {
        var incident = await db.Incidents.FindAsync(incidentId);
        if (incident is null) return NotFound();

        incident.Severity = request.Severity;
        incident.Title = request.Title.Trim();
        incident.Description = request.Description.Trim();
        incident.AssignedTo = CleanOptional(request.AssignedTo);
        incident.Status = request.Status;
        incident.SlaDueAt = request.SlaDueAt;
        incident.ResolvedAt = request.Status == IncidentStatus.Resolved ? DateTime.UtcNow : null;
        incident.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
