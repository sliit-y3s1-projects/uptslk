using api.Common.Helpers;
using api.DTO.Routes;
using api.DTO.Trips;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using RouteDirection = api.Entities.RouteDirection;

namespace api.Services;

public sealed class RouteScheduleService(
    IRouteScheduleRepository schedules,
    IRouteRepository routes,
    IRouteDirectionRepository directions,
    IBayRepository bays,
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    ITripRepository trips,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TripConflictService conflicts,
    MobileNotificationService notifications) : IRouteScheduleService
{
    private const string RouteNotFound = "Route not found.";
    private const string ScheduleNotFound = "Schedule not found.";
    private const string InvalidRange = "Last departure must be later than first departure.";
    private const string BayOutsideCentre = "The selected bay must belong to this direction's departure centre.";

    public async Task<IReadOnlyList<RouteScheduleResponse>> ListAsync(Guid routeId, CancellationToken cancellationToken)
    {
        if (!await routes.ExistsAsync(routeId, cancellationToken)) throw new NotFoundException(RouteNotFound);
        var found = await schedules.ListForRouteAsync(routeId, cancellationToken);
        return found.Select(schedule => RouteScheduleResponse.From(schedule, schedule.Bay)).ToList();
    }

    public async Task<RouteScheduleResponse> CreateAsync(Guid routeId, CreateRouteScheduleRequest request, CancellationToken cancellationToken)
    {
        var route = await routes.GetReadOnlyAsync(routeId, cancellationToken) ?? throw new NotFoundException(RouteNotFound);
        currentUser.EnsureCanManageCentre(route.CentreId);
        if (request.LastDeparture <= request.FirstDeparture) throw new BadRequestException(InvalidRange);

        RouteDirection? direction = null;
        if (request.RouteDirectionId.HasValue)
        {
            direction = await directions.GetForRouteAsync(request.RouteDirectionId.Value, routeId, cancellationToken)
                ?? throw new BadRequestException("The selected direction does not belong to this route.");
            if (!direction.IsActive) throw new BadRequestException("The selected direction is inactive.");
        }
        var departureCentreId = direction?.StartCentreId ?? route.CentreId;
        var bay = await bays.GetInCentreAsync(request.BayId, departureCentreId, cancellationToken) ?? throw new BadRequestException(BayOutsideCentre);

        var schedule = new RouteSchedule
        {
            RouteId = routeId,
            RouteDirectionId = request.RouteDirectionId,
            BayId = request.BayId,
            FirstDeparture = request.FirstDeparture,
            LastDeparture = request.LastDeparture,
            HeadwayMinutes = request.HeadwayMinutes,
            OperatingDays = request.OperatingDays.Trim()
        };
        schedules.Add(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RouteScheduleResponse.From(schedule, bay);
    }

    public async Task UpdateAsync(Guid scheduleId, UpdateRouteScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await GetManagedScheduleAsync(scheduleId, cancellationToken);
        if (request.LastDeparture <= request.FirstDeparture) throw new BadRequestException(InvalidRange);

        var departureCentreId = schedule.RouteDirection?.StartCentreId ?? schedule.Route.CentreId;
        if (await bays.GetInCentreAsync(request.BayId, departureCentreId, cancellationToken) is null)
            throw new BadRequestException(BayOutsideCentre);

        schedule.BayId = request.BayId;
        schedule.FirstDeparture = request.FirstDeparture;
        schedule.LastDeparture = request.LastDeparture;
        schedule.HeadwayMinutes = request.HeadwayMinutes;
        schedule.OperatingDays = request.OperatingDays.Trim();
        schedule.IsActive = request.IsActive;
        schedule.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await GetManagedScheduleAsync(scheduleId, cancellationToken);
        schedule.IsActive = false;
        schedule.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<GeneratedTripsResponse> GenerateTripsAsync(Guid scheduleId, GenerateScheduleTripsRequest request, CancellationToken cancellationToken)
    {
        var schedule = await GetManagedScheduleAsync(scheduleId, cancellationToken);
        if (!schedule.IsActive) throw new BadRequestException("Only active schedules can generate trips.");
        if (schedule.HeadwayMinutes <= 0) throw new BadRequestException("The schedule must have a positive departure interval.");
        if (!OperatesOn(schedule.OperatingDays, request.ServiceDate))
            throw new BadRequestException("This schedule does not operate on the selected date.");

        var centreId = schedule.Route.CentreId;
        var activeVehicles = await vehicles.ListActiveInCentreAsync(centreId, cancellationToken);
        var activeDrivers = await drivers.ListActiveInCentreAsync(centreId, cancellationToken);
        if (activeVehicles.Count == 0 || activeDrivers.Count == 0)
            throw new BadRequestException("Add at least one active vehicle and driver before generating trips.");
        if (schedule.Bay.Status != BayStatus.Available)
            throw new BadRequestException("The schedule's bay is not available for dispatch.");

        var duration = schedule.RouteDirection?.EstimatedDurationMin ?? schedule.Route.EstimatedDurationMin;
        var existing = 0;
        var createdDepartures = new List<string>();
        var createdTrips = new List<Trip>();
        var skipped = new List<SkippedDeparture>();
        var sequence = 0;

        foreach (var departure in ScheduleTimes.Departures(schedule.FirstDeparture, schedule.LastDeparture, schedule.HeadwayMinutes))
        {
            var time = departure.ToString("HH:mm");
            var scheduledTime = SriLankaTime.ToUtc(request.ServiceDate.ToDateTime(departure, DateTimeKind.Unspecified));

            if (await trips.ExistsAtAsync(schedule.RouteId, schedule.RouteDirectionId, scheduledTime, cancellationToken))
            {
                existing++;
                skipped.Add(new SkippedDeparture(time, "A trip is already scheduled at this time."));
            }
            else if (await FindFreeAssignmentAsync(schedule, activeVehicles, activeDrivers, sequence, scheduledTime, duration, cancellationToken) is { } trip)
            {
                // Saved one at a time so the next departure's conflict check can see this trip.
                trips.Add(trip);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                createdTrips.Add(trip);
                createdDepartures.Add(time);
            }
            else
            {
                skipped.Add(new SkippedDeparture(time, "No free bus, driver, and bay combination was available."));
            }
            sequence++;
        }

        if (createdTrips.Count > 0)
        {
            await notifications.TripsAssignedAsync(createdTrips, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var conflictCount = skipped.Count - existing;
        return new GeneratedTripsResponse(
            createdTrips.Count + skipped.Count,
            createdTrips.Count,
            existing,
            conflictCount,
            request.ServiceDate,
            createdDepartures,
            skipped);
    }

    // Rotates through vehicles and drivers so the work is shared, and returns the first pair with no clash.
    private async Task<Trip?> FindFreeAssignmentAsync(
        RouteSchedule schedule, List<Vehicle> activeVehicles, List<Driver> activeDrivers,
        int sequence, DateTime scheduledTime, int duration, CancellationToken cancellationToken)
    {
        for (var vehicleOffset = 0; vehicleOffset < activeVehicles.Count; vehicleOffset++)
        {
            var vehicle = activeVehicles[(sequence + vehicleOffset) % activeVehicles.Count];
            for (var driverOffset = 0; driverOffset < activeDrivers.Count; driverOffset++)
            {
                var driver = activeDrivers[(sequence + driverOffset) % activeDrivers.Count];
                var clashes = await conflicts.FindConflicts(vehicle.Id, driver.Id, schedule.BayId, scheduledTime, duration, cancellationToken: cancellationToken);
                if (clashes.Count > 0) continue;

                return new Trip
                {
                    CentreId = schedule.Route.CentreId,
                    RouteId = schedule.RouteId,
                    RouteDirectionId = schedule.RouteDirectionId,
                    VehicleId = vehicle.Id,
                    DriverId = driver.Id,
                    BayId = schedule.BayId,
                    ScheduledTime = scheduledTime,
                    Notes = "Generated from recurring timetable."
                };
            }
        }
        return null;
    }

    private async Task<RouteSchedule> GetManagedScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await schedules.GetWithDetailsAsync(scheduleId, cancellationToken) ?? throw new NotFoundException(ScheduleNotFound);
        currentUser.EnsureCanManageCentre(schedule.Route.CentreId);
        return schedule;
    }

    // Operating days are "Everyday", "Weekdays", "Weekends" or a list such as "Mon,Wed,Fri".
    private static bool OperatesOn(string operatingDays, DateOnly date)
    {
        if (operatingDays.Equals("Everyday", StringComparison.OrdinalIgnoreCase)) return true;
        if (operatingDays.Equals("Weekdays", StringComparison.OrdinalIgnoreCase)) return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
        if (operatingDays.Equals("Weekends", StringComparison.OrdinalIgnoreCase)) return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var day = date.DayOfWeek.ToString()[..3];
        return operatingDays.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(item => item.Equals(day, StringComparison.OrdinalIgnoreCase));
    }
}
