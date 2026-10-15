using api.Common.Constants;
using api.DTO.Maintenance;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/maintenance-records")]
public class MaintenanceRecordsController(IMaintenanceRecordService maintenanceRecords) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaintenanceRecordListItemResponse>>> List([FromQuery] MaintenanceRecordFilter filter, CancellationToken cancellationToken) =>
        Ok(await maintenanceRecords.ListAsync(filter, cancellationToken));

    [HttpGet("{recordId:guid}")]
    public async Task<ActionResult<MaintenanceRecordResponse>> Get(Guid recordId, CancellationToken cancellationToken) =>
        Ok(await maintenanceRecords.GetAsync(recordId, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AppRoles.FleetStaff)]
    public async Task<ActionResult<MaintenanceRecordCreatedResponse>> Create(CreateMaintenanceRecordRequest request, CancellationToken cancellationToken)
    {
        var created = await maintenanceRecords.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { recordId = created.Id }, created);
    }

    [HttpPut("{recordId:guid}")]
    [Authorize(Roles = AppRoles.FleetStaff)]
    public async Task<IActionResult> Update(Guid recordId, UpdateMaintenanceRecordRequest request, CancellationToken cancellationToken)
    {
        await maintenanceRecords.UpdateAsync(recordId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{recordId:guid}")]
    [Authorize(Roles = AppRoles.FleetStaff)]
    public async Task<IActionResult> Cancel(Guid recordId, CancellationToken cancellationToken)
    {
        await maintenanceRecords.CancelAsync(recordId, cancellationToken);
        return NoContent();
    }
}
