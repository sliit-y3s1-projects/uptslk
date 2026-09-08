using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

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

public sealed class CreateBayRequest
{
    [Required, StringLength(24)] public string Code { get; init; } = default!;
    [StringLength(100)] public string? Name { get; init; }
    public BayStatus Status { get; init; } = BayStatus.Available;
}

public sealed class UpdateBayRequest
{
    [Required, StringLength(24)] public string Code { get; init; } = default!;
    [StringLength(100)] public string? Name { get; init; }
    public BayStatus Status { get; init; }
}
