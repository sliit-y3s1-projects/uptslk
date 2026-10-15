using System.ComponentModel.DataAnnotations;
using api.Entities.Enums;

namespace api.DTO.Centres;

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
