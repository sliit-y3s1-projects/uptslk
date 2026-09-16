using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/centres")]
public class CentresController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] CentreStatus? status, [FromQuery] string? district, [FromQuery] string? search, [FromQuery] bool includeClosed = false)
    {
        var query = db.Centres.AsNoTracking().AsQueryable();

        if (!includeClosed && !status.HasValue) query = query.Where(centre => centre.Status != CentreStatus.Closed);

        if (status.HasValue) query = query.Where(centre => centre.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(district)) query = query.Where(centre => centre.District == district.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(centre => centre.Code.ToLower().Contains(term) || centre.Name.ToLower().Contains(term) || centre.City.ToLower().Contains(term));
        }

        var centres = await query
            .OrderBy(centre => centre.Name)
            .Select(centre => new
            {
                centre.Id,
                centre.Code,
                centre.Name,
                centre.City,
                centre.District,
                centre.Status,
                BayCount = centre.Bays.Count,
                RouteCount = centre.Routes.Count(route => route.IsActive)
            })
            .ToListAsync();

        return Ok(centres);
    }

    [HttpGet("{centreId:guid}")]
    public async Task<IActionResult> Get(Guid centreId)
    {
        var centre = await db.Centres.AsNoTracking()
            .Include(item => item.Bays)
            .Include(item => item.Routes)
            .SingleOrDefaultAsync(item => item.Id == centreId);

        if (centre is null) return NotFound();

        return Ok(new
        {
            centre.Id,
            centre.Code,
            centre.Name,
            centre.City,
            centre.District,
            centre.Description,
            centre.Status,
            Bays = centre.Bays.OrderBy(bay => bay.Code).Select(bay => new { bay.Id, bay.Code, bay.Name, bay.Status }),
            Routes = centre.Routes.OrderBy(route => route.RouteNumber).Select(route => new { route.Id, route.RouteNumber, route.Name, route.Origin, route.Destination, route.IsActive })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCentreRequest request)
    {
        var code = NormalizeCode(request.Code);
        if (await db.Centres.AnyAsync(centre => centre.Code == code)) return Conflict(new { error = "A centre with this code already exists." });

        var centre = new Centre
        {
            Code = code,
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            District = request.District.Trim(),
            Description = CleanOptional(request.Description),
            Status = request.Status
        };

        db.Centres.Add(centre);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { centreId = centre.Id }, new { centre.Id, centre.Code, centre.Name, centre.Status });
    }

    [HttpPut("{centreId:guid}")]
    public async Task<IActionResult> Update(Guid centreId, UpdateCentreRequest request)
    {
        var centre = await db.Centres.FindAsync(centreId);
        if (centre is null) return NotFound();

        centre.Name = request.Name.Trim();
        centre.City = request.City.Trim();
        centre.District = request.District.Trim();
        centre.Description = CleanOptional(request.Description);
        centre.Status = request.Status;
        centre.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{centreId:guid}")]
    public async Task<IActionResult> Close(Guid centreId)
    {
        var centre = await db.Centres.FindAsync(centreId);
        if (centre is null) return NotFound();

        centre.Status = CentreStatus.Closed;
        centre.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{centreId:guid}/bays")]
    public async Task<IActionResult> ListBays(Guid centreId)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == centreId)) return NotFound();

        var bays = await db.Bays.AsNoTracking()
            .Where(bay => bay.CentreId == centreId)
            .OrderBy(bay => bay.Code)
            .Select(bay => new { bay.Id, bay.CentreId, bay.Code, bay.Name, bay.Status })
            .ToListAsync();

        return Ok(bays);
    }

    [HttpPost("{centreId:guid}/bays")]
    public async Task<IActionResult> CreateBay(Guid centreId, CreateBayRequest request)
    {
        if (!await db.Centres.AnyAsync(centre => centre.Id == centreId)) return NotFound();

        var code = NormalizeCode(request.Code);
        if (await db.Bays.AnyAsync(bay => bay.CentreId == centreId && bay.Code == code)) return Conflict(new { error = "A bay with this code already exists at this centre." });

        var bay = new Bay { CentreId = centreId, Code = code, Name = CleanOptional(request.Name), Status = request.Status };
        db.Bays.Add(bay);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBay), new { bayId = bay.Id }, new { bay.Id, bay.CentreId, bay.Code, bay.Name, bay.Status });
    }

    [HttpGet("bays/{bayId:guid}")]
    public async Task<IActionResult> GetBay(Guid bayId)
    {
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == bayId);
        if (bay is null) return NotFound();
        return Ok(new { bay.Id, bay.CentreId, bay.Code, bay.Name, bay.Status });
    }

    [HttpPut("bays/{bayId:guid}")]
    public async Task<IActionResult> UpdateBay(Guid bayId, UpdateBayRequest request)
    {
        var bay = await db.Bays.FindAsync(bayId);
        if (bay is null) return NotFound();

        var code = NormalizeCode(request.Code);
        if (await db.Bays.AnyAsync(item => item.Id != bayId && item.CentreId == bay.CentreId && item.Code == code)) return Conflict(new { error = "A bay with this code already exists at this centre." });

        bay.Code = code;
        bay.Name = CleanOptional(request.Name);
        bay.Status = request.Status;
        bay.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("bays/{bayId:guid}")]
    public async Task<IActionResult> DeactivateBay(Guid bayId)
    {
        var bay = await db.Bays.FindAsync(bayId);
        if (bay is null) return NotFound();

        bay.Status = BayStatus.OutOfService;
        bay.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
