namespace api.DTOs;

public record RegisterRequest(string Name, string Email, string Password);
public record CreateUserRequest(string Name, string Email, string Password, string Role, Guid? CentreId);
public record AssignUserCentreRequest(Guid? CentreId);
public record UpdateUserDetailsRequest(string Name, string Email);
public record LoginRequest(string Email, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateProfileRequest(string Name, string? HomeLocation = null, string? NicNumber = null, string? Gender = null);
public record VerifyNicRequest(string NicNumber);
public record AuthResponse(string Token, string UserId, string Name, string Email, string Role, Guid? CentreId, string? ProfilePhotoUrl = null);
