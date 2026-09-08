using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/fare-rules")]
public class FareRulesController(AppDbContext db) : ControllerBase
{
    [HttpGet("quote")]
    public async Task<IActionResult> Quote([FromQuery] Guid tripId, [FromQuery] Guid passengerId)
    {
        var trip = await db.Trips.AsNoTracking().Include(item => item.Route).SingleOrDefaultAsync(item => item.Id == tripId);
        var passenger = await db.Passengers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == passengerId && item.IsActive);
        if (trip is null) return BadRequest(new { error = "The selected trip does not exist." });
        if (passenger is null) return BadRequest(new { error = "The selected passenger is not active." });

        var rule = await db.FareRules.AsNoTracking().SingleOrDefaultAsync(item => item.RouteId == trip.RouteId && item.PassengerCategory == passenger.Category && item.IsActive);
        if (rule is null) return NotFound(new { error = "No active fare rule exists for this passenger category and route." });

        return Ok(new
        {
            Trip = new { trip.Id, trip.ScheduledTime, Route = trip.Route.RouteNumber, trip.Route.Name },
            Passenger = new { passenger.Id, passenger.Category },
            FareRuleId = rule.Id,
            Fare = rule.Amount
        });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? routeId, [FromQuery] Guid? centreId, [FromQuery] PassengerCategory? passengerCategory, [FromQuery] bool? active)
    {
        var query = db.FareRules.AsNoTracking().Include(rule => rule.Route).ThenInclude(route => route.Centre).AsQueryable();
        if (routeId.HasValue) query = query.Where(rule => rule.RouteId == routeId.Value);
        if (centreId.HasValue) query = query.Where(rule => rule.Route.CentreId == centreId.Value);
        if (passengerCategory.HasValue) query = query.Where(rule => rule.PassengerCategory == passengerCategory.Value);
        if (active.HasValue) query = query.Where(rule => rule.IsActive == active.Value);

        var rules = await query.OrderBy(rule => rule.Route.RouteNumber).ThenBy(rule => rule.PassengerCategory).Select(rule => new
        {
            rule.Id,
            rule.RouteId,
            Route = new { rule.Route.RouteNumber, rule.Route.Name, rule.Route.CentreId },
            rule.PassengerCategory,
            rule.Amount,
            rule.IsActive,
            rule.UpdatedAt
        }).ToListAsync();
        return Ok(rules);
    }

    [HttpGet("{fareRuleId:guid}")]
    public async Task<IActionResult> Get(Guid fareRuleId)
    {
        var rule = await db.FareRules.AsNoTracking().Include(item => item.Route).ThenInclude(route => route.Centre).SingleOrDefaultAsync(item => item.Id == fareRuleId);
        if (rule is null) return NotFound();
        return Ok(new
        {
            rule.Id,
            rule.RouteId,
            Route = new { rule.Route.Id, rule.Route.RouteNumber, rule.Route.Name, rule.Route.Origin, rule.Route.Destination, rule.Route.CentreId },
            rule.PassengerCategory,
            rule.Amount,
            rule.IsActive,
            rule.CreatedAt,
            rule.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFareRuleRequest request)
    {
        if (!await db.Routes.AnyAsync(route => route.Id == request.RouteId && route.IsActive)) return BadRequest(new { error = "The selected active route does not exist." });
        if (await db.FareRules.AnyAsync(rule => rule.RouteId == request.RouteId && rule.PassengerCategory == request.PassengerCategory)) return Conflict(new { error = "A fare rule already exists for this passenger category on the selected route." });

        var rule = new FareRule { RouteId = request.RouteId, PassengerCategory = request.PassengerCategory, Amount = request.Amount };
        db.FareRules.Add(rule);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { fareRuleId = rule.Id }, new { rule.Id, rule.RouteId, rule.PassengerCategory, rule.Amount, rule.IsActive });
    }

    [HttpPut("{fareRuleId:guid}")]
    public async Task<IActionResult> Update(Guid fareRuleId, UpdateFareRuleRequest request)
    {
        var rule = await db.FareRules.FindAsync(fareRuleId);
        if (rule is null) return NotFound();
        if (await db.FareRules.AnyAsync(item => item.Id != fareRuleId && item.RouteId == rule.RouteId && item.PassengerCategory == request.PassengerCategory)) return Conflict(new { error = "A fare rule already exists for this passenger category on the selected route." });

        rule.PassengerCategory = request.PassengerCategory;
        rule.Amount = request.Amount;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{fareRuleId:guid}")]
    public async Task<IActionResult> Deactivate(Guid fareRuleId)
    {
        var rule = await db.FareRules.FindAsync(fareRuleId);
        if (rule is null) return NotFound();
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
