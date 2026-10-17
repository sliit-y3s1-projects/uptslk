namespace api.Services.AgentRecovery.Copilot;

// How a recovery option is scored out of 100. Each factor is a pure function so it is easy to test.
public static class RecoveryScoring
{
    public const double CapacityMax = 40;
    public const double DelayMax = 25;
    public const double SlackMax = 25;
    public const double MemoryMax = 10;

    public const int TurnaroundMinutes = 10;
    public const int ComfortableSlackMinutes = 60;
    public const int TightSlackMinutes = 15;
    private const double RejectionPenalty = 15;
    private const double ApprovalBonus = 3;
    private const double NoHistoryPoints = 7;

    // Full marks for a bus up to 1.5x the passenger count. Bigger buses waste capacity, a bus with under 5% spare is tight.
    public static double Capacity(int capacity, int passengers)
    {
        if (capacity < passengers) return 0;
        var needed = Math.Max(passengers, 1);
        var ratio = (double)capacity / needed;
        var points = ratio <= 1.5 ? CapacityMax : CapacityMax - Math.Min(25, (ratio - 1.5) * 10);
        if (passengers > 0 && (capacity - passengers) / (double)capacity < 0.05) points -= 10;
        return Math.Max(0, points);
    }

    // Every minute of delay costs the same share of the points. 90 minutes or more scores nothing.
    public static double Delay(int delayMinutes) =>
        Math.Max(0, DelayMax * (1 - delayMinutes / 90.0));

    // Spare minutes before the vehicle or driver is needed again. No next trip counts as comfortable.
    public static double Slack(int? slackMinutes)
    {
        if (slackMinutes is null) return SlackMax;
        return SlackMax * Math.Clamp(slackMinutes.Value / (double)ComfortableSlackMinutes, 0, 1);
    }

    public static double Memory(int rejections, bool previouslyApproved)
    {
        var points = NoHistoryPoints - rejections * RejectionPenalty + (previouslyApproved ? ApprovalBonus : 0);
        return Math.Clamp(points, -30, MemoryMax);
    }

    public static double Total(params double[] factors) => Math.Round(Math.Clamp(factors.Sum(), 0, 100), 1);
}
