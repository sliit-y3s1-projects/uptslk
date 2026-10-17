using System.ComponentModel.DataAnnotations;

namespace api.DTO.AgentRecovery;

public enum ImpactReviewDecision
{
    Ignore,
    Escalate,
    Reset
}

public sealed class SimulateScenarioRequest
{
    public Guid VehicleId { get; init; }
    // Left empty, the agent picks the first conflict-free driver.
    public Guid? DriverId { get; init; }
    // Left empty, the agent picks an available bay.
    public Guid? BayId { get; init; }
    [Range(0, 120)] public int DelayMinutes { get; init; } = 15;
}

public sealed class ExplainImpactRequest
{
    public Guid TripId { get; init; }
    [StringLength(500)] public string? Question { get; init; }
}

public sealed class ReviewImpactRequest
{
    public Guid TripId { get; init; }
    public ImpactReviewDecision Decision { get; init; }
}

public sealed class SaveMessageDraftRequest
{
    [StringLength(200)] public string? Subject { get; init; }
    [Required, StringLength(900, MinimumLength = 10)] public string Message { get; init; } = default!;
}

public sealed class RegenerateMessageRequest
{
    [Required, StringLength(300, MinimumLength = 3)] public string Instruction { get; init; } = default!;
}

public sealed class ReviseRecoveryRequest
{
    [Required, StringLength(2000, MinimumLength = 3)] public string Reason { get; init; } = default!;
    public bool AvoidVehicle { get; init; }
    public bool AvoidDriver { get; init; }
    public bool AvoidBay { get; init; }
    [Range(0, 120)] public int? MaxDelayMinutes { get; init; }
}
