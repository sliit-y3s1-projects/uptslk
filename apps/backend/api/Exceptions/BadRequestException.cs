namespace api.Exceptions;

public sealed class BadRequestException(string message, IReadOnlyList<string>? errors = null)
    : AppException(message, StatusCodes.Status400BadRequest)
{
    // Extra messages when one request breaks more than one rule.
    public IReadOnlyList<string> Errors { get; } = errors ?? [];
}
