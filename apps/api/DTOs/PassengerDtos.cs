using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class CreatePassengerRequest
{
    [Required, StringLength(160)] public string FullName { get; init; } = default!;
    [Required, StringLength(32)] public string PhoneNumber { get; init; } = default!;
    [EmailAddress, StringLength(256)] public string? Email { get; init; }
    [StringLength(100, MinimumLength = 8)] public string? Password { get; init; }
    public PassengerCategory Category { get; init; } = PassengerCategory.Adult;
}

public sealed class UpdatePassengerRequest
{
    [Required, StringLength(160)] public string FullName { get; init; } = default!;
    [Required, StringLength(32)] public string PhoneNumber { get; init; } = default!;
    [EmailAddress, StringLength(256)] public string? Email { get; init; }
    public PassengerCategory Category { get; init; }
    public bool IsActive { get; init; }
}

public sealed class TopUpWalletRequest
{
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Amount { get; init; }
}

public sealed class ResetPassengerPasswordRequest
{
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; init; } = default!;
}
