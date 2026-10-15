namespace api.Common.Helpers;

public static class Text
{
    // Trims the value, or returns null when it is empty or only spaces.
    public static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
