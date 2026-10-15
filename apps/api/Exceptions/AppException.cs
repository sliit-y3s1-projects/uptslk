namespace api.Exceptions;

// Base class for errors that are safe to show to the API caller.
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    // Extra fields added to the error response, for example the ids of conflicting trips.
    public Dictionary<string, object?> Details { get; } = [];

    // For older services that report failures as a status code and a message.
    public static AppException FromStatusCode(int statusCode, string message) => statusCode switch
    {
        StatusCodes.Status400BadRequest => new BadRequestException(message),
        StatusCodes.Status401Unauthorized => new UnauthorizedException(message),
        StatusCodes.Status403Forbidden => new ForbiddenException(message),
        StatusCodes.Status404NotFound => new NotFoundException(message),
        StatusCodes.Status409Conflict => new ConflictException(message),
        StatusCodes.Status502BadGateway => new BadGatewayException(message),
        StatusCodes.Status503ServiceUnavailable => new ServiceUnavailableException(message),
        _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "No exception type exists for this status code.")
    };
}
