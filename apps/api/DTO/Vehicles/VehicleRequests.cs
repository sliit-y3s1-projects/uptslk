using System.ComponentModel.DataAnnotations;
using api.Entities.Enums;

namespace api.DTO.Vehicles;

public sealed class CreateVehicleRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(32)] public string PlateNumber { get; init; } = default!;
    [Required, StringLength(120)] public string Model { get; init; } = default!;
    public VehicleType Type { get; init; }
    [Range(1, 200)] public int Capacity { get; init; }
    public bool IsAccessible { get; init; }
    public VehicleStatus Status { get; init; } = VehicleStatus.Active;
}

public sealed class UpdateVehicleRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(32)] public string PlateNumber { get; init; } = default!;
    [Required, StringLength(120)] public string Model { get; init; } = default!;
    public VehicleType Type { get; init; }
    [Range(1, 200)] public int Capacity { get; init; }
    public bool IsAccessible { get; init; }
    public VehicleStatus Status { get; init; }
}
