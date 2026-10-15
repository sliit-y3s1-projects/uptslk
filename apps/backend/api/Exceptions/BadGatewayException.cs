namespace api.Exceptions;

// An outside service we called returned an error.
public sealed class BadGatewayException(string message)
    : AppException(message, StatusCodes.Status502BadGateway);
