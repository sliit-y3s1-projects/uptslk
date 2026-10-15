using api.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace api.Common.Extensions;

public static class IdentityResultExtensions
{
    // Throws a BadRequestException with Identity's messages when the operation failed.
    public static void ThrowIfFailed(this IdentityResult result)
    {
        if (result.Succeeded) return;
        var errors = result.Errors.Select(error => error.Description).ToList();
        throw new BadRequestException(string.Join(" ", errors), errors);
    }
}
