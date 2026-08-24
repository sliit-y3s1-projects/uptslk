namespace api.DTOs;

public record RegisterRequest(string Name, string Email, string Password);
public record CreateUserRequest(string Name, string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string UserId, string Name, string Email, string Role);