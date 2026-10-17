using api.Entities;
using api.Entities.Enums;

namespace api.DTO.AgentRecovery;

public sealed record ScoreFactorResponse(string Factor, double Points, double MaxPoints, string Detail);

// One way to recover the trip, tried out by the fleet agent and scored out of 100.
public sealed record RecoveryAlternativeResponse(
    string Id,
    Guid VehicleId,
    string VehiclePlate,
    int VehicleCapacity,
    Guid? DriverId,
    string? DriverName,
    Guid? BayId,
    string? BayCode,
    DateTime ScheduledTime,
    int DelayMinutes,
    double Score,
    bool Feasible,
    bool IsCurrent,
    IReadOnlyList<ScoreFactorResponse> Factors,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Issues);

public sealed record RecoveryLessonResponse(Guid Id, string Kind, string Summary, string? Reason, DateTime CreatedAt)
{
    public static RecoveryLessonResponse From(RecoveryMemory memory) =>
        new(memory.Id, memory.Kind.ToString(), memory.Summary, memory.Reason, memory.CreatedAt);
}

public sealed record RecoveryAlternativesResponse(
    IReadOnlyList<RecoveryAlternativeResponse> Alternatives,
    int Evaluated,
    string Recommendation,
    IReadOnlyList<RecoveryLessonResponse> Lessons);

public sealed record ImpactNodeResponse(
    Guid TripId,
    string Lane,
    int Depth,
    string RouteNumber,
    string RouteName,
    DateTime ScheduledTime,
    string Risk,
    int? SlackMinutes,
    int ExpectedLateMinutes,
    int Passengers,
    string? Vehicle,
    string? Driver,
    string Reason,
    string? Review);

public sealed record ImpactSummaryResponse(int High, int Medium, int Low, int Ignored, int Escalated, int PassengersAtRisk);

public sealed record RecoveryImpactResponse(
    DateTime RecoveredDeparture,
    DateTime RecoveredArrival,
    int DelayMinutes,
    IReadOnlyList<ImpactNodeResponse> Nodes,
    ImpactSummaryResponse Summary,
    string Recommendation);

public sealed record ImpactExplanationResponse(string Answer, string Source);

public sealed record MessageSegmentResponse(
    string Segment,
    string Label,
    string Description,
    int Recipients,
    bool CanReceiveInApp,
    string Subject,
    string Message,
    string Source,
    IReadOnlyList<string> Warnings,
    int Delivered,
    int Failed);

public sealed record RecoveryMessagesResponse(
    IReadOnlyList<MessageSegmentResponse> Segments,
    int TotalRecipients,
    bool Sent);

public sealed record ReviseRecoveryResponse(
    Guid Id,
    WorkflowStatus Status,
    string? FailureReason,
    int Revision,
    string Rationale,
    IReadOnlyList<string> AppliedConstraints,
    int AlternativesConsidered);
