namespace api.Exceptions;

// Base class for errors that are safe to show to the API caller.
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
