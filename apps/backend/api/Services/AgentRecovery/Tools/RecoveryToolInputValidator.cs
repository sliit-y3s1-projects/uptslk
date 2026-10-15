namespace api.Services.AgentRecovery.Tools;

internal static class RecoveryToolInputValidator
{
    public static void RequireId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
    }

    public static void RequireNonNegative(int value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, "The value cannot be negative.");
    }

    public static void RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "The value must be greater than zero.");
    }
}
