namespace api.DTO.Auth;

public record AuthResponse(string Token, string UserId, string Name, string Email, string Role, Guid? CentreId, string? ProfilePhotoUrl = null);
