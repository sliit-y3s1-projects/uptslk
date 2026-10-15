using System.ComponentModel.DataAnnotations;

namespace api.DTO.Routes;

public class CreateRouteDirectionRequest
{
    public Guid StartCentreId { get; init; }
    public Guid EndCentreId { get; init; }
    [Required, StringLength(160)] public string Name { get; init; } = default!;
    [Range(0.1, 5000)] public decimal DistanceKm { get; init; }
    [Range(1, 1440)] public int EstimatedDurationMin { get; init; }
    [MinLength(2)] public List<RouteStopInput> Stops { get; init; } = [];
}

public sealed class UpdateRouteDirectionRequest : CreateRouteDirectionRequest
{
    public bool IsActive { get; init; } = true;
}
