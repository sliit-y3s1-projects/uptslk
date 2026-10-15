using api.Common.Constants;
using api.DTO.Fares;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/fare-rules")]
public class FareRulesController(IFareRuleService fareRules) : ControllerBase
{
    [HttpGet("quote")]
    public async Task<ActionResult<FareQuoteResponse>> Quote([FromQuery] Guid tripId, [FromQuery] Guid passengerId, CancellationToken cancellationToken) =>
        Ok(await fareRules.QuoteAsync(tripId, passengerId, cancellationToken));

    [Authorize(Roles = AppRoles.Commuter)]
    [HttpGet("quote/me")]
    public async Task<ActionResult<FareQuoteResponse>> QuoteForCurrentUser([FromQuery] Guid tripId, CancellationToken cancellationToken) =>
        Ok(await fareRules.QuoteForCurrentUserAsync(tripId, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FareRuleListItemResponse>>> List([FromQuery] FareRuleFilter filter, CancellationToken cancellationToken) =>
        Ok(await fareRules.ListAsync(filter, cancellationToken));

    [HttpGet("{fareRuleId:guid}")]
    public async Task<ActionResult<FareRuleResponse>> Get(Guid fareRuleId, CancellationToken cancellationToken) =>
        Ok(await fareRules.GetAsync(fareRuleId, cancellationToken));

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPost]
    public async Task<ActionResult<FareRuleCreatedResponse>> Create(CreateFareRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await fareRules.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { fareRuleId = created.Id }, created);
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpPut("{fareRuleId:guid}")]
    public async Task<IActionResult> Update(Guid fareRuleId, UpdateFareRuleRequest request, CancellationToken cancellationToken)
    {
        await fareRules.UpdateAsync(fareRuleId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Managers)]
    [HttpDelete("{fareRuleId:guid}")]
    public async Task<IActionResult> Deactivate(Guid fareRuleId, CancellationToken cancellationToken)
    {
        await fareRules.DeactivateAsync(fareRuleId, cancellationToken);
        return NoContent();
    }
}
