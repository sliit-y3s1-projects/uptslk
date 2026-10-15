using api.Common.Constants;
using api.DTO.Incidents;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/incidents")]
[Authorize(Roles = AppRoles.OperationsStaff)]
public class IncidentsController(IIncidentService incidents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentListItemResponse>>> List([FromQuery] IncidentFilter filter, CancellationToken cancellationToken) =>
        Ok(await incidents.ListAsync(filter, cancellationToken));

    [HttpGet("{incidentId:guid}")]
    public async Task<ActionResult<IncidentResponse>> Get(Guid incidentId, CancellationToken cancellationToken) =>
        Ok(await incidents.GetAsync(incidentId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<IncidentCreatedResponse>> Create(CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        var created = await incidents.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { incidentId = created.Id }, created);
    }

    [HttpPut("{incidentId:guid}")]
    public async Task<IActionResult> Update(Guid incidentId, UpdateIncidentRequest request, CancellationToken cancellationToken)
    {
        await incidents.UpdateAsync(incidentId, request, cancellationToken);
        return NoContent();
    }
}
