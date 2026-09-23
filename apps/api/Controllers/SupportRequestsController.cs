using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/support-requests")]
public class SupportRequestsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SupportRequestType? type, [FromQuery] SupportRequestStatus? status, [FromQuery] Guid? passengerId, [FromQuery] Guid? tripId)
    {
        var query = db.SupportRequests.AsNoTracking()
            .Include(request => request.Passenger)
            .Include(request => request.Trip).ThenInclude(trip => trip!.Route)
            .AsQueryable();
        if (type.HasValue) query = query.Where(request => request.Type == type.Value);
        if (status.HasValue) query = query.Where(request => request.Status == status.Value);
        if (passengerId.HasValue) query = query.Where(request => request.PassengerId == passengerId.Value);
        if (tripId.HasValue) query = query.Where(request => request.TripId == tripId.Value);
        return Ok(await query.OrderByDescending(request => request.CreatedAt).Select(request => new
        {
            request.Id, request.PassengerId,
            Passenger = request.Passenger == null ? null : request.Passenger.FullName,
            request.TripId,
            Trip = request.Trip == null ? null : request.Trip.Route.RouteNumber,
            request.CentreId, request.Type, request.Priority, request.Status,
            request.Subject, request.Description, request.AssignedTo, request.Resolution,
            request.CreatedAt, request.UpdatedAt, request.ResolvedAt
        }).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var request = await db.SupportRequests.AsNoTracking()
            .Include(item => item.Passenger).Include(item => item.Trip).ThenInclude(trip => trip!.Route)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return NotFound(new { error = "Support request not found." });
        return Ok(request);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupportRequest input)
    {
        if (input.PassengerId.HasValue && !await db.Passengers.AnyAsync(passenger => passenger.Id == input.PassengerId.Value))
            return BadRequest(new { error = "The selected passenger does not exist." });
        if (input.TripId.HasValue && !await db.Trips.AnyAsync(trip => trip.Id == input.TripId.Value))
            return BadRequest(new { error = "The selected trip does not exist." });
        var request = new SupportRequest
        {
            PassengerId = input.PassengerId, TripId = input.TripId, CentreId = input.CentreId,
            Type = input.Type, Priority = input.Priority, Subject = input.Subject.Trim(),
            Description = input.Description.Trim(), AssignedTo = Clean(input.AssignedTo)
        };
        db.SupportRequests.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = request.Id }, new { request.Id, request.Status });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSupportRequest input)
    {
        var request = await db.SupportRequests.FindAsync(id);
        if (request is null) return NotFound(new { error = "Support request not found." });
        request.Priority = input.Priority; request.Status = input.Status;
        request.Subject = input.Subject.Trim(); request.Description = input.Description.Trim();
        request.AssignedTo = Clean(input.AssignedTo); request.Resolution = Clean(input.Resolution);
        request.ResolvedAt = input.Status is SupportRequestStatus.Resolved or SupportRequestStatus.Closed ? DateTime.UtcNow : null;
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
