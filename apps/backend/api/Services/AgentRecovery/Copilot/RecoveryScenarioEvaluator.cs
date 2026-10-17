using api.DTO.AgentRecovery;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services.AgentRecovery.Copilot;

public sealed record ScenarioContext(
    Trip Trip,
    IncidentType IncidentType,
    int Passengers,
    int DurationMinutes,
    bool ExcludeOriginals,
    IReadOnlyList<Vehicle> Vehicles,
    IReadOnlyList<Driver> Drivers,
    IReadOnlyList<Bay> Bays,
    IReadOnlyList<Trip> WindowTrips,
    IReadOnlyList<RecoveryMemory> Lessons,
    RecoveryProposal? Current);

public sealed record ScenarioSearchOptions(
    IReadOnlySet<Guid> ExcludedVehicles,
    IReadOnlySet<Guid> ExcludedDrivers,
    IReadOnlySet<Guid> ExcludedBays,
    int? MaxDelayMinutes,
    int Take)
{
    public static ScenarioSearchOptions Default { get; } =
        new(new HashSet<Guid>(), new HashSet<Guid>(), new HashSet<Guid>(), null, 5);
}

public sealed record ScenarioSearchResult(IReadOnlyList<RecoveryAlternativeResponse> Alternatives, int Evaluated);

// The fleet agent's simulator. It tries a bus, driver, bay and delay against the real timetable and scores the result.
public sealed class RecoveryScenarioEvaluator(
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IBayRepository bays,
    ITripRepository trips,
    ITripConflictService conflicts,
    RecoveryMemoryService memory)
{
    private const int MaximumDelayMinutes = 120;
    private const int MaximumVehiclesTried = 4;
    private static readonly int[] DelayOptions = [15, 30, 45];

    public async Task<ScenarioContext> LoadAsync(Trip trip, IncidentType incidentType, int passengers, RecoveryProposal? current, CancellationToken cancellationToken)
    {
        var departureCentreId = trip.RouteDirection?.StartCentreId ?? trip.CentreId;
        return new ScenarioContext(
            trip,
            incidentType,
            passengers,
            trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin,
            incidentType is IncidentType.Breakdown or IncidentType.Safety,
            await vehicles.ListActiveInCentreWithMaintenanceAsync(trip.CentreId, cancellationToken),
            await drivers.ListActiveInCentreAsync(trip.CentreId, cancellationToken),
            await bays.ListAvailableInCentreAsync(departureCentreId, cancellationToken),
            await trips.ListActiveInWindowAsync(trip.CentreId, trip.ScheduledTime.AddHours(-2), trip.ScheduledTime.AddHours(10), cancellationToken),
            await memory.GetLessonsAsync(trip.CentreId, trip.Route.RouteNumber, incidentType, cancellationToken),
            current);
    }

    public async Task<ScenarioSearchResult> SearchAsync(ScenarioContext context, ScenarioSearchOptions options, CancellationToken cancellationToken)
    {
        var delays = DelayOptions.Where(delay => delay <= (options.MaxDelayMinutes ?? MaximumDelayMinutes)).ToList();
        if (delays.Count == 0) delays.Add(Math.Clamp(options.MaxDelayMinutes ?? 0, 0, MaximumDelayMinutes));

        // Buses that carry everyone come first, smallest first so large buses stay free for other routes.
        var candidates = context.Vehicles
            .Where(vehicle => !options.ExcludedVehicles.Contains(vehicle.Id))
            .Where(vehicle => !context.ExcludeOriginals || vehicle.Id != context.Trip.VehicleId)
            .OrderByDescending(vehicle => vehicle.Capacity >= context.Passengers)
            .ThenBy(vehicle => vehicle.Capacity)
            .Take(MaximumVehiclesTried)
            .ToList();

        var results = new List<RecoveryAlternativeResponse>();
        var evaluated = 0;
        foreach (var vehicle in candidates)
        {
            foreach (var delay in delays)
            {
                evaluated++;
                var result = await EvaluateAsync(context, vehicle.Id, null, null, delay, options, cancellationToken);
                if (result.Feasible) results.Add(result);
            }
        }

        // The agent's own proposal is always shown, so the coordinator can compare it with the rest.
        if (context.Current is not null)
        {
            var delay = Math.Max(0, (int)(context.Current.ScheduledTime - context.Trip.ScheduledTime).TotalMinutes);
            var alreadyListed = results.Any(result => result.VehicleId == context.Current.VehicleId
                && result.DriverId == context.Current.DriverId && result.DelayMinutes == delay);
            if (!alreadyListed && !options.ExcludedVehicles.Contains(context.Current.VehicleId))
            {
                evaluated++;
                var current = await EvaluateAsync(context, context.Current.VehicleId, context.Current.DriverId, context.Current.BayId, delay, options, cancellationToken);
                if (current.Feasible) results.Add(current);
            }
        }

        var ranked = results.OrderByDescending(result => result.Score).ThenBy(result => result.DelayMinutes).ToList();
        var top = ranked.Take(options.Take).ToList();
        foreach (var current in ranked.Where(result => result.IsCurrent && !top.Contains(result))) top.Add(current);
        return new ScenarioSearchResult(top.OrderByDescending(result => result.Score).ToList(), evaluated);
    }

    public async Task<RecoveryAlternativeResponse> EvaluateAsync(
        ScenarioContext context,
        Guid vehicleId,
        Guid? driverId,
        Guid? bayId,
        int delayMinutes,
        ScenarioSearchOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= ScenarioSearchOptions.Default;
        var trip = context.Trip;
        var issues = new List<string>();
        var risks = new List<string>();
        var time = trip.ScheduledTime.AddMinutes(delayMinutes);
        var vehicle = context.Vehicles.FirstOrDefault(candidate => candidate.Id == vehicleId);
        if (vehicle is null)
        {
            issues.Add("This is not an active vehicle at this centre.");
            return Infeasible(vehicleId, "Unknown vehicle", 0, time, delayMinutes, issues);
        }

        if (delayMinutes is < 0 or > MaximumDelayMinutes)
            issues.Add($"The delay must be between 0 and {MaximumDelayMinutes} minutes.");
        if (context.ExcludeOriginals && vehicle.Id == trip.VehicleId)
            issues.Add("This is the affected vehicle.");
        if (vehicle.MaintenanceRecords.Any(record => VehicleMaintenanceRules.BlocksTrip(record, time, context.DurationMinutes)))
            issues.Add("This bus has scheduled or in-progress maintenance during the trip.");
        if (vehicle.Capacity < context.Passengers)
            issues.Add($"Capacity {vehicle.Capacity} cannot carry {context.Passengers} passengers.");

        var bay = ResolveBay(context, vehicle, bayId, delayMinutes, options, issues);
        Driver? driver = null;
        if (issues.Count == 0 && bay is not null)
            driver = await ResolveDriverAsync(context, vehicle, bay, driverId, time, delayMinutes, options, issues, cancellationToken);
        else if (driverId.HasValue)
            driver = context.Drivers.FirstOrDefault(candidate => candidate.Id == driverId.Value);

        if (issues.Count > 0)
            return Infeasible(vehicle.Id, vehicle.PlateNumber, vehicle.Capacity, time, delayMinutes, issues, driver, bay);

        var end = time.AddMinutes(context.DurationMinutes);
        var vehicleNext = NextTrip(context, candidate => candidate.VehicleId == vehicle.Id, time);
        var driverNext = NextTrip(context, candidate => candidate.DriverId == driver!.Id, time);
        var vehicleSlack = SlackBefore(vehicleNext, end);
        var driverSlack = SlackBefore(driverNext, end);
        var tightest = new[] { vehicleSlack, driverSlack }.Where(slack => slack.HasValue).Select(slack => slack!.Value).DefaultIfEmpty(int.MaxValue).Min();
        int? slackMinutes = tightest == int.MaxValue ? null : tightest;
        if (slackMinutes < RecoveryScoring.TightSlackMinutes)
        {
            var next = vehicleSlack == slackMinutes ? vehicleNext : driverNext;
            risks.Add($"Only {slackMinutes} min before route {next!.Route.RouteNumber} departs at {next.ScheduledTime:HH:mm} UTC, so any further delay would cascade.");
        }

        var history = RecoveryMemoryService.Assess(vehicle.Id, driver!.Id, bay!.Id, delayMinutes, context.Lessons);
        risks.AddRange(history.Notes.Where(_ => history.Rejections > 0));

        var capacityPoints = RecoveryScoring.Capacity(vehicle.Capacity, context.Passengers);
        var delayPoints = RecoveryScoring.Delay(delayMinutes);
        var slackPoints = RecoveryScoring.Slack(slackMinutes);
        var memoryPoints = RecoveryScoring.Memory(history.Rejections, history.PreviouslyApproved);
        var factors = new List<ScoreFactorResponse>
        {
            new("Capacity fit", Round(capacityPoints), RecoveryScoring.CapacityMax, $"{vehicle.Capacity} seats for {context.Passengers} passengers"),
            new("Delay", Round(delayPoints), RecoveryScoring.DelayMax, delayMinutes == 0 ? "Departs on time" : $"Departs {delayMinutes} min late"),
            new("Slack before next duty", Round(slackPoints), RecoveryScoring.SlackMax, slackMinutes is null ? "Neither the bus nor the driver has another trip soon" : $"{slackMinutes} min spare after this trip"),
            new("Past decisions", Round(memoryPoints), RecoveryScoring.MemoryMax, history.Notes.Count == 0 ? "No earlier decisions for this combination" : history.Notes[0])
        };

        var current = context.Current;
        return new RecoveryAlternativeResponse(
            $"{vehicle.Id:N}-{driver.Id:N}-{delayMinutes}",
            vehicle.Id,
            vehicle.PlateNumber,
            vehicle.Capacity,
            driver.Id,
            driver.FullName,
            bay.Id,
            bay.Code,
            time,
            delayMinutes,
            RecoveryScoring.Total(capacityPoints, delayPoints, slackPoints, memoryPoints),
            true,
            current is not null && current.VehicleId == vehicle.Id && current.DriverId == driver.Id && current.ScheduledTime == time,
            factors,
            risks,
            []);
    }

    private static Bay? ResolveBay(ScenarioContext context, Vehicle vehicle, Guid? bayId, int delayMinutes, ScenarioSearchOptions options, List<string> issues)
    {
        if (bayId.HasValue)
        {
            var requested = context.Bays.FirstOrDefault(candidate => candidate.Id == bayId.Value);
            if (requested is null) issues.Add("The chosen bay is not available.");
            return requested;
        }

        // A bay a manager rejected before is the last choice, not the first.
        var open = context.Bays
            .Where(candidate => !options.ExcludedBays.Contains(candidate.Id))
            .OrderBy(candidate => RecoveryMemoryService.Assess(vehicle.Id, null, candidate.Id, delayMinutes, context.Lessons).Rejections)
            .ThenBy(candidate => candidate.Id == context.Trip.BayId)
            .ToList();
        var bay = open.FirstOrDefault();
        if (bay is null) issues.Add("No bay is available at the departure centre.");
        return bay;
    }

    private async Task<Driver?> ResolveDriverAsync(
        ScenarioContext context,
        Vehicle vehicle,
        Bay bay,
        Guid? driverId,
        DateTime time,
        int delayMinutes,
        ScenarioSearchOptions options,
        List<string> issues,
        CancellationToken cancellationToken)
    {
        var trip = context.Trip;
        if (driverId.HasValue)
        {
            var requested = context.Drivers.FirstOrDefault(candidate => candidate.Id == driverId.Value);
            if (requested is null)
            {
                issues.Add("This driver is not active at this centre.");
                return null;
            }

            if (context.ExcludeOriginals && requested.Id == trip.DriverId) issues.Add("This is the affected driver.");
            issues.AddRange(await conflicts.FindConflicts(vehicle.Id, requested.Id, bay.Id, time, context.DurationMinutes, trip.Id, cancellationToken));
            return requested;
        }

        IReadOnlyList<string> lastConflicts = [];
        // A driver a manager rejected before is tried last.
        var eligible = context.Drivers
            .Where(candidate => !options.ExcludedDrivers.Contains(candidate.Id) && (!context.ExcludeOriginals || candidate.Id != trip.DriverId))
            .OrderBy(candidate => RecoveryMemoryService.Assess(vehicle.Id, candidate.Id, bay.Id, delayMinutes, context.Lessons).Rejections);
        foreach (var candidate in eligible)
        {
            lastConflicts = await conflicts.FindConflicts(vehicle.Id, candidate.Id, bay.Id, time, context.DurationMinutes, trip.Id, cancellationToken);
            if (lastConflicts.Count == 0) return candidate;
        }

        issues.Add(lastConflicts.Count > 0
            ? $"No driver is free for this bus, bay and time. {lastConflicts[0]}"
            : "No eligible driver is available.");
        return null;
    }

    private static Trip? NextTrip(ScenarioContext context, Func<Trip, bool> usesResource, DateTime from) =>
        context.WindowTrips
            .Where(candidate => candidate.Id != context.Trip.Id && usesResource(candidate) && candidate.ScheduledTime >= from)
            .OrderBy(candidate => candidate.ScheduledTime)
            .FirstOrDefault();

    private static int? SlackBefore(Trip? next, DateTime end) =>
        next is null ? null : (int)(next.ScheduledTime - end).TotalMinutes - RecoveryScoring.TurnaroundMinutes;

    private static double Round(double value) => Math.Round(value, 1);

    private static RecoveryAlternativeResponse Infeasible(
        Guid vehicleId,
        string plate,
        int capacity,
        DateTime time,
        int delayMinutes,
        IReadOnlyList<string> issues,
        Driver? driver = null,
        Bay? bay = null) =>
        new($"{vehicleId:N}-{driver?.Id:N}-{delayMinutes}", vehicleId, plate, capacity, driver?.Id, driver?.FullName, bay?.Id, bay?.Code, time, delayMinutes, 0, false, false, [], [], issues);
}
