namespace api.Exceptions;

public sealed class ForbiddenException(string message = "You do not have access to this resource.")
    : AppException(message, StatusCodes.Status403Forbidden);
