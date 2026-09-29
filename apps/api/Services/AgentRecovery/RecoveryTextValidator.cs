using System.Text;

namespace api.Services.AgentRecovery;

public static class RecoveryTextValidator
{
    private const int MaximumObjectiveLength = 1000;
    private const int MaximumDecisionNoteLength = 2000;
    private const int MaximumLines = 12;

    public static bool TryNormalizeObjective(
        string? value,
        out string? normalized,
        out string? error) =>
        TryNormalizeOptional(value, MaximumObjectiveLength, "Recovery objective", out normalized, out error);

    public static bool TryNormalizeDecisionNote(
        string? value,
        out string? normalized,
        out string? error) =>
        TryNormalizeOptional(value, MaximumDecisionNoteLength, "Decision note", out normalized, out error);

    public static string SanitizeForPrompt(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(Math.Min(value.Length, maximumLength));
        foreach (var character in value.Trim())
        {
            if (builder.Length >= maximumLength) break;
            if (!char.IsControl(character) || character is '\n' or '\r' or '\t')
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool TryNormalizeOptional(
        string? value,
        int maximumLength,
        string fieldName,
        out string? normalized,
        out string? error)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        error = null;
        if (normalized is null) return true;
        if (normalized.Length > maximumLength)
        {
            error = $"{fieldName} cannot exceed {maximumLength} characters.";
            return false;
        }

        if (normalized.Count(character => character == '\n') + 1 > MaximumLines)
        {
            error = $"{fieldName} cannot exceed {MaximumLines} lines.";
            return false;
        }

        if (normalized.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
        {
            error = $"{fieldName} contains unsupported control characters.";
            return false;
        }

        return true;
    }
}
