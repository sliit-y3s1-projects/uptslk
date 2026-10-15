namespace api.Exceptions;

public sealed class ConflictException(string message)
    : AppException(message, StatusCodes.Status409Conflict);
