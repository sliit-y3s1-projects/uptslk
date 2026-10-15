using System.ComponentModel.DataAnnotations;
using api.Entities.Enums;

namespace api.DTO.Drivers;

public sealed class CreateDriverRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(160)] public string FullName { get; init; } = default!;
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = default!;
    [Required, MinLength(8), StringLength(128)] public string Password { get; init; } = default!;
    [StringLength(32)] public string? PhoneNumber { get; init; }
    [Required, StringLength(64)] public string LicenseNumber { get; init; } = default!;
    public DriverStatus Status { get; init; } = DriverStatus.Active;
}

public sealed class UpdateDriverRequest
{
    public Guid CentreId { get; init; }
    [Required, StringLength(160)] public string FullName { get; init; } = default!;
    [StringLength(32)] public string? PhoneNumber { get; init; }
    public DriverStatus Status { get; init; }
}
