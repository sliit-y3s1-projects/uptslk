namespace api.Exceptions;

public sealed class UnauthorizedException(string message = "You need to sign in to do this.")
    : AppException(message, StatusCodes.Status401Unauthorized);
