using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;

namespace api.Services.AgentRecovery.Copilot;

public sealed record RejectionConstraints(bool AvoidVehicle, bool AvoidDriver, bool AvoidBay, int? MaxDelayMinutes)
{
    public static RejectionConstraints None { get; } = new(false, false, false, null);
}

public sealed record MemoryAssessment(int Rejections, bool PreviouslyApproved, IReadOnlyList<string> Notes);

// Remembers what managers decided on earlier recoveries, so the same mistake is not proposed twice.
public sealed class RecoveryMemoryService(
    IRecoveryMemoryRepository memories,
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IBayRepository bays)
{
    private const int LessonLimit = 30;

    public Task<List<RecoveryMemory>> GetLessonsAsync(Guid centreId, string routeNumber, IncidentType incidentType, CancellationToken cancellationToken) =>
        memories.ListForContextAsync(centreId, routeNumber, incidentType, LessonLimit, cancellationToken);

    // Added to the current unit of work. The caller saves it together with the decision.
    public async Task RecordAsync(
        AgentWorkflow workflow,
        RecoveryMemoryKind kind,
        RecoveryProposal proposal,
        string? reason,
        RejectionConstraints constraints,
        CancellationToken cancellationToken)
    {
        var delay = CurrentDelay(workflow, proposal);
        memories.Add(new RecoveryMemory
        {
            CentreId = workflow.CentreId,
            WorkflowId = workflow.Id,
            Kind = kind,
            IncidentType = workflow.Incident.Type,
            RouteNumber = workflow.Trip.Route.RouteNumber,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Summary = await DescribeAsync(proposal, delay, cancellationToken),
            VehicleId = proposal.VehicleId,
            DriverId = proposal.DriverId,
            BayId = proposal.BayId,
            DelayMinutes = delay,
            AvoidVehicle = constraints.AvoidVehicle,
            AvoidDriver = constraints.AvoidDriver,
            AvoidBay = constraints.AvoidBay,
            MaxDelayMinutes = constraints.MaxDelayMinutes
        });
    }

    // Works out what a manager wants avoided. Ticked boxes win. Otherwise the reason text is read, and if it says nothing
    // specific the exact bus and driver of the rejected proposal are avoided.
    public async Task<(RejectionConstraints Constraints, bool Inferred)> ResolveRejectionAsync(
        string? reason,
        bool avoidVehicle,
        bool avoidDriver,
        bool avoidBay,
        int? maxDelayMinutes,
        RecoveryProposal proposal,
        int currentDelay,
        CancellationToken cancellationToken)
    {
        if (avoidVehicle || avoidDriver || avoidBay || maxDelayMinutes.HasValue)
            return (new RejectionConstraints(avoidVehicle, avoidDriver, avoidBay, maxDelayMinutes), false);

        var vehicle = await vehicles.GetReadOnlyAsync(proposal.VehicleId, cancellationToken);
        var driver = await drivers.GetReadOnlyAsync(proposal.DriverId, cancellationToken);
        return (Interpret(reason, currentDelay, vehicle?.PlateNumber, driver?.FullName), true);
    }

    public static RejectionConstraints Interpret(string? reason, int currentDelay, string? plate, string? driverName)
    {
        var text = (reason ?? "").ToLowerInvariant();
        var driver = ContainsAny(text, "driver", "fatigue", "rest ", "shift", "overtime", "hours", "licen")
            || (driverName is not null && driverName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(part => part.Length >= 3 && text.Contains(part.ToLowerInvariant())));
        var vehicle = ContainsAny(text, "bus", "vehicle", "plate", "tyre", "tire", "maintenance", "condition", "capacity", "seats")
            || (plate is not null && text.Contains(plate.ToLowerInvariant()));
        var bay = text.Contains("bay");
        int? maxDelay = null;
        var minutes = System.Text.RegularExpressions.Regex.Match(text, @"(\d{1,3})\s*(min|minutes)");
        if (minutes.Success && int.TryParse(minutes.Groups[1].Value, out var stated)) maxDelay = Math.Clamp(stated, 0, 120);
        else if (ContainsAny(text, "late", "delay", "wait", "too long", "sooner", "earlier", "faster")) maxDelay = Math.Max(0, currentDelay - 15);

        if (!driver && !vehicle && !bay && maxDelay is null) return new RejectionConstraints(true, true, false, null);
        return new RejectionConstraints(vehicle, driver, bay, maxDelay);
    }

    public async Task<string> DescribeAsync(RecoveryProposal proposal, int delayMinutes, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetReadOnlyAsync(proposal.VehicleId, cancellationToken);
        var driver = await drivers.GetReadOnlyAsync(proposal.DriverId, cancellationToken);
        var bay = await bays.GetReadOnlyAsync(proposal.BayId, cancellationToken);
        return $"Bus {vehicle?.PlateNumber ?? "unknown"}, driver {driver?.FullName ?? "unknown"}, bay {bay?.Code ?? "unknown"}, {delayMinutes} min delay";
    }

    // How earlier decisions bear on one candidate. Each rejection counts once however many of its points match.
    public static MemoryAssessment Assess(Guid vehicleId, Guid? driverId, Guid? bayId, int delayMinutes, IEnumerable<RecoveryMemory> lessons)
    {
        var rejections = 0;
        var approved = false;
        var notes = new List<string>();
        foreach (var lesson in lessons)
        {
            if (lesson.Kind == RecoveryMemoryKind.Rejected)
            {
                var matches = new List<string>();
                if (lesson.AvoidVehicle && lesson.VehicleId == vehicleId) matches.Add("this bus");
                if (lesson.AvoidDriver && driverId.HasValue && lesson.DriverId == driverId) matches.Add("this driver");
                if (lesson.AvoidBay && bayId.HasValue && lesson.BayId == bayId) matches.Add("this bay");
                if (lesson.MaxDelayMinutes.HasValue && delayMinutes > lesson.MaxDelayMinutes) matches.Add($"a delay over {lesson.MaxDelayMinutes} min");
                if (matches.Count == 0) continue;
                rejections++;
                var why = string.IsNullOrWhiteSpace(lesson.Reason) ? "" : $": \"{Shorten(lesson.Reason)}\"";
                notes.Add($"A manager rejected {string.Join(" and ", matches)} on {lesson.CreatedAt:yyyy-MM-dd}{why}.");
            }
            else if (lesson.VehicleId == vehicleId && driverId.HasValue && lesson.DriverId == driverId)
            {
                approved = true;
                notes.Add($"This bus and driver pairing was approved on {lesson.CreatedAt:yyyy-MM-dd}.");
            }
        }

        return new MemoryAssessment(rejections, approved, notes);
    }

    // Short, plain lines for the planner prompt. Free text from managers is trimmed and kept as data.
    public static IReadOnlyList<string> ToPromptLessons(IEnumerable<RecoveryMemory> lessons) =>
        lessons.Take(8)
            .Select(lesson => $"{lesson.Kind} on {lesson.CreatedAt:yyyy-MM-dd}: {lesson.Summary}"
                + (string.IsNullOrWhiteSpace(lesson.Reason) ? "" : $". Reason given: {RecoveryTextValidator.SanitizeForPrompt(Shorten(lesson.Reason), 160)}"))
            .ToArray();

    // Minutes between the trip's original departure and the proposal. Once a recovery is applied the trip already holds the new time.
    public static int CurrentDelay(AgentWorkflow workflow, RecoveryProposal proposal) =>
        workflow.Status == WorkflowStatus.Completed ? 0 : Math.Max(0, (int)(proposal.ScheduledTime - workflow.Trip.ScheduledTime).TotalMinutes);

    private static bool ContainsAny(string text, params string[] words) => words.Any(text.Contains);

    private static string Shorten(string value) => value.Length <= 120 ? value : value[..117] + "...";
}
