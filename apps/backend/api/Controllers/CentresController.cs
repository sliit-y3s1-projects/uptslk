using api.Common.Constants;
using api.DTO.Centres;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/centres")]
public class CentresController(ICentreService centres) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CentreListItemResponse>>> List([FromQuery] CentreFilter filter, CancellationToken cancellationToken) =>
        Ok(await centres.ListAsync(filter, cancellationToken));

    [HttpGet("{centreId:guid}")]
    public async Task<ActionResult<CentreResponse>> Get(Guid centreId, CancellationToken cancellationToken) =>
        Ok(await centres.GetAsync(centreId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost]
    public async Task<ActionResult<CentreCreatedResponse>> Create(CreateCentreRequest request, CancellationToken cancellationToken)
    {
        var created = await centres.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { centreId = created.Id }, created);
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPut("{centreId:guid}")]
    public async Task<IActionResult> Update(Guid centreId, UpdateCentreRequest request, CancellationToken cancellationToken)
    {
        await centres.UpdateAsync(centreId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpDelete("{centreId:guid}")]
    public async Task<IActionResult> Close(Guid centreId, CancellationToken cancellationToken)
    {
        await centres.CloseAsync(centreId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{centreId:guid}/bays")]
    public async Task<ActionResult<IReadOnlyList<BayResponse>>> ListBays(Guid centreId, CancellationToken cancellationToken) =>
        Ok(await centres.ListBaysAsync(centreId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost("{centreId:guid}/bays")]
    public async Task<ActionResult<BayResponse>> CreateBay(Guid centreId, CreateBayRequest request, CancellationToken cancellationToken)
    {
        var created = await centres.CreateBayAsync(centreId, request, cancellationToken);
        return CreatedAtAction(nameof(GetBay), new { bayId = created.Id }, created);
    }

    [HttpGet("bays/{bayId:guid}")]
    public async Task<ActionResult<BayResponse>> GetBay(Guid bayId, CancellationToken cancellationToken) =>
        Ok(await centres.GetBayAsync(bayId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPut("bays/{bayId:guid}")]
    public async Task<IActionResult> UpdateBay(Guid bayId, UpdateBayRequest request, CancellationToken cancellationToken)
    {
        await centres.UpdateBayAsync(bayId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpDelete("bays/{bayId:guid}")]
    public async Task<IActionResult> DeactivateBay(Guid bayId, CancellationToken cancellationToken)
    {
        await centres.DeactivateBayAsync(bayId, cancellationToken);
        return NoContent();
    }
}
