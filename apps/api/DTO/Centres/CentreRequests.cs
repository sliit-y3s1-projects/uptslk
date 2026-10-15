using System.ComponentModel.DataAnnotations;
using api.Entities.Enums;

namespace api.DTO.Centres;

public sealed class CreateCentreRequest
{
    [Required, StringLength(32)] public string Code { get; init; } = default!;
    [Required, StringLength(160)] public string Name { get; init; } = default!;
    [Required, StringLength(100)] public string City { get; init; } = default!;
    [Required, StringLength(100)] public string District { get; init; } = default!;
    [StringLength(1000)] public string? Description { get; init; }
    public CentreStatus Status { get; init; } = CentreStatus.Planned;
}

public sealed class UpdateCentreRequest
{
    [Required, StringLength(160)] public string Name { get; init; } = default!;
    [Required, StringLength(100)] public string City { get; init; } = default!;
    [Required, StringLength(100)] public string District { get; init; } = default!;
    [StringLength(1000)] public string? Description { get; init; }
    public CentreStatus Status { get; init; }
}
