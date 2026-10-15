using api.Common.Constants;
using api.DTO.Support;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.DispatchStaff)]
[Route("api/v1/support-requests")]
public class SupportRequestsController(ISupportRequestService supportRequests) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupportRequestResponse>>> List([FromQuery] SupportRequestFilter filter, CancellationToken cancellationToken) =>
        Ok(await supportRequests.ListAsync(filter, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupportRequestResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await supportRequests.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SupportRequestCreatedResponse>> Create(CreateSupportRequest input, CancellationToken cancellationToken)
    {
        var created = await supportRequests.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSupportRequest input, CancellationToken cancellationToken)
    {
        await supportRequests.UpdateAsync(id, input, cancellationToken);
        return NoContent();
    }
}
