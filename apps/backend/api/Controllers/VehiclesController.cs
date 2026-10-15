using api.Common.Constants;
using api.DTO.Vehicles;
using api.Interfaces.Services;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
public class VehiclesController(IVehicleService vehicles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleListItemResponse>>> List([FromQuery] VehicleFilter filter, CancellationToken cancellationToken) =>
        Ok(await vehicles.ListAsync(filter, cancellationToken));

    [HttpGet("{vehicleId:guid}")]
    public async Task<ActionResult<VehicleResponse>> Get(Guid vehicleId, CancellationToken cancellationToken) =>
        Ok(await vehicles.GetAsync(vehicleId, cancellationToken));

    [Authorize(Roles = AppRoles.FleetStaff)]
    [HttpPost]
    public async Task<ActionResult<VehicleCreatedResponse>> Create(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var created = await vehicles.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { vehicleId = created.Id }, created);
    }

    [Authorize(Roles = AppRoles.FleetStaff)]
    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> Update(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        await vehicles.UpdateAsync(vehicleId, request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.FleetStaff)]
    [HttpDelete("{vehicleId:guid}")]
    public async Task<IActionResult> Deactivate(Guid vehicleId, CancellationToken cancellationToken)
    {
        await vehicles.DeactivateAsync(vehicleId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{vehicleId:guid}/image")]
    [Authorize(Roles = AppRoles.FleetStaff)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(VehicleService.MaxImageBytes + 1024 * 1024)]
    public async Task<ActionResult<VehicleImageResponse>> UploadImage(Guid vehicleId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await vehicles.UploadImageAsync(vehicleId, stream.ToArray(), file.ContentType, cancellationToken));
    }
}
