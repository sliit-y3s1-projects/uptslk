using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public class VehicleCreateDto
{
    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(32, MinimumLength = 2, ErrorMessage = "Registration number must be between 2 and 32 characters.")]
    public string RegistrationNumber { get; set; } = default!;

    [Required]
    public VehicleType VehicleType { get; set; } = VehicleType.Normal;

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
    public int Capacity { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    [Required(ErrorMessage = "Centre is required.")]
    public Guid CentreId { get; set; }

    [StringLength(160)]
    public string? CentreName { get; set; }

    [StringLength(120)]
    public string? Model { get; set; }
}

public class VehicleUpdateDto
{
    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(32, MinimumLength = 2, ErrorMessage = "Registration number must be between 2 and 32 characters.")]
    public string RegistrationNumber { get; set; } = default!;

    [Required]
    public VehicleType VehicleType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
    public int Capacity { get; set; }

    public VehicleStatus Status { get; set; }

    [Required(ErrorMessage = "Centre is required.")]
    public Guid CentreId { get; set; }

    [StringLength(160)]
    public string? CentreName { get; set; }

    [StringLength(120)]
    public string? Model { get; set; }

    public bool? IsActive { get; set; }
}

public class VehicleResponseDto
{
    public Guid VehicleId { get; set; }
    public string RegistrationNumber { get; set; } = default!;
    public VehicleType VehicleType { get; set; }
    public int Capacity { get; set; }
    public VehicleStatus Status { get; set; }
    public Guid CentreId { get; set; }
    public string? CentreName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Model { get; set; }
}

