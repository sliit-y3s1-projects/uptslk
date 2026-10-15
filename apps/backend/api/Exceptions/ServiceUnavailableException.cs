namespace api.Exceptions;

// An outside service we depend on (storage, AI planner, payments) is not set up or not reachable.
public sealed class ServiceUnavailableException(string message)
    : AppException(message, StatusCodes.Status503ServiceUnavailable);
