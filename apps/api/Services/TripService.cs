using api.Common.Helpers;
using api.DTO.Trips;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class TripService(
    ITripRepository trips,
    IBookingRepository bookings,
    ICentreRepository centres,
    IRouteRepository routes,
    IRouteDirectionRepository directions,
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IBayRepository bays,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TripConflictService conflicts,
    BookingCancellationService cancellation,
    MobileNotificationService notifications) : ITripService
{
    private const string NotFoundMessage = "Trip not found.";

    public async Task<IReadOnlyList<TripListItemResponse>> ListAsync(TripFilter filter, CancellationToken cancellationToken) =>
        await ToListItemsAsync(await trips.SearchAsync(filter, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<TripListItemResponse>> ListHistoryAsync(TripHistoryFilter filter, CancellationToken cancellationToken) =>
        await ToListItemsAsync(await trips.GetHistoryAsync(filter, cancellationToken), cancellationToken);

    public async Task<TripResponse> GetAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await trips.GetWithDetailsAsync(tripId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        return TripResponse.From(trip);
    }

    public async Task<TripCreatedResponse> CreateAsync(CreateTripRequest request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanManageCentre(request.CentreId);
        await EnsureValidAssignmentAsync(new Assignment(
            request.CentreId, request.RouteId, request.RouteDirectionId, request.VehicleId, request.DriverId, request.BayId, request.ScheduledTime),
            cancellationToken);

        var trip = new Trip
        {
            CentreId = request.CentreId,
            RouteId = request.RouteId,
            RouteDirectionId = request.RouteDirectionId,
            VehicleId = request.VehicleId,
            DriverId = request.DriverId,
            BayId = request.BayId,
            ScheduledTime = request.ScheduledTime,
            Notes = Text.CleanOptional(request.Notes)
        };
        trips.Add(trip);
        await notifications.TripAssignedAsync(trip, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new TripCreatedResponse(trip.Id, trip.CentreId, trip.RouteId, trip.ScheduledTime, trip.Status);
    }

    public async Task UpdateAsync(Guid tripId, UpdateTripRequest request, CancellationToken cancellationToken)
    {
        var trip = await GetManagedTripAsync(tripId, cancellationToken);
        // Moving a trip needs access to both the old and the new centre.
        currentUser.EnsureCanManageCentre(request.CentreId);
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new BadRequestException("Completed or cancelled trips cannot be edited.");

        await EnsureValidAssignmentAsync(new Assignment(
            request.CentreId, request.RouteId, request.RouteDirectionId, request.VehicleId, request.DriverId, request.BayId, request.ScheduledTime, tripId),
            cancellationToken);

        var before = TripAssignment.Of(trip);
        trip.CentreId = request.CentreId;
        trip.RouteId = request.RouteId;
        trip.RouteDirectionId = request.RouteDirectionId;
        trip.VehicleId = request.VehicleId;
        trip.DriverId = request.DriverId;
        trip.BayId = request.BayId;
        trip.ScheduledTime = request.ScheduledTime;
        trip.Notes = Text.CleanOptional(request.Notes);
        trip.UpdatedAt = DateTime.UtcNow;
        await notifications.TripChangedAsync(trip, before, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReassignAsync(Guid tripId, ReassignTripRequest request, CancellationToken cancellationToken)
    {
        var trip = await GetManagedTripAsync(tripId, cancellationToken);
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new BadRequestException("Completed or cancelled trips cannot be reassigned.");

        await EnsureValidAssignmentAsync(new Assignment(
            trip.CentreId, trip.RouteId, trip.RouteDirectionId, request.VehicleId, request.DriverId, request.BayId, request.ScheduledTime, tripId),
            cancellationToken);

        var before = TripAssignment.Of(trip);
        trip.VehicleId = request.VehicleId;
        trip.DriverId = request.DriverId;
        trip.BayId = request.BayId;
        trip.ScheduledTime = request.ScheduledTime;
        trip.Notes = Text.CleanOptional(request.Notes) ?? trip.Notes;
        trip.UpdatedAt = DateTime.UtcNow;
        await notifications.TripChangedAsync(trip, before, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid tripId, UpdateTripStatusRequest request, CancellationToken cancellationToken)
    {
        var trip = await GetManagedTripAsync(tripId, cancellationToken);
        if (!CanTransition(trip.Status, request.Status))
            throw new BadRequestException($"Cannot change a {trip.Status} trip to {request.Status}.");

        var note = Text.CleanOptional(request.Note);
        if (request.Status == TripStatus.Cancelled)
        {
            await CancelWithRefundsAsync(tripId, note ?? "Cancelled by dispatch", note, cancellationToken);
            return;
        }

        trip.Status = request.Status;
        trip.Notes = note ?? trip.Notes;
        if (request.Status == TripStatus.Dispatched) trip.ActualDepartureAt = DateTime.UtcNow;
        if (request.Status == TripStatus.Completed) trip.CompletedAt = DateTime.UtcNow;
        trip.UpdatedAt = DateTime.UtcNow;
        await notifications.TripStatusChangedAsync(trip, note, changedByDriver: false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid tripId, CancelTripRequest request, CancellationToken cancellationToken)
    {
        var trip = await GetManagedTripAsync(tripId, cancellationToken);
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new BadRequestException("Only active trips can be cancelled.");

        await CancelWithRefundsAsync(tripId, request.Reason.Trim(), null, cancellationToken);
    }

    // Cancelling a trip also cancels every active booking on it and returns the passengers' money.
    private async Task CancelWithRefundsAsync(Guid tripId, string reason, string? note, CancellationToken cancellationToken)
    {
        var result = await cancellation.CancelTripAsync(tripId, reason, note, cancellationToken);
        if (!result.Cancelled) throw AppException.FromStatusCode(result.StatusCode, result.Error ?? NotFoundMessage);
    }

    private async Task<Trip> GetManagedTripAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await trips.GetByIdAsync(tripId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        currentUser.EnsureCanManageCentre(trip.CentreId);
        return trip;
    }

    private async Task<IReadOnlyList<TripListItemResponse>> ToListItemsAsync(List<Trip> found, CancellationToken cancellationToken)
    {
        var occupied = await bookings.GetOccupiedCapacityByTripAsync(found.Select(trip => trip.Id).ToList(), cancellationToken);
        return found.Select(trip => TripListItemResponse.From(trip, occupied.GetValueOrDefault(trip.Id))).ToList();
    }

    // Checks every part of a trip assignment and reports all problems at once.
    private async Task EnsureValidAssignmentAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var centre = await centres.GetByIdAsync(assignment.CentreId, cancellationToken);
        var route = await routes.GetByIdAsync(assignment.RouteId, cancellationToken);
        var direction = assignment.DirectionId.HasValue ? await directions.GetByIdAsync(assignment.DirectionId.Value, cancellationToken) : null;
        var vehicle = await vehicles.GetByIdAsync(assignment.VehicleId, cancellationToken);
        var driver = await drivers.GetByIdAsync(assignment.DriverId, cancellationToken);
        var bay = await bays.GetByIdAsync(assignment.BayId, cancellationToken);

        if (centre is null) errors.Add("The selected centre does not exist.");
        else if (centre.Status != CentreStatus.Operating) errors.Add("Trips can only be scheduled at an operating centre.");
        if (route is null) errors.Add("The selected route does not exist.");
        else if (!route.IsActive) errors.Add("The selected route is inactive.");
        if (assignment.DirectionId.HasValue && direction is null) errors.Add("The selected direction does not exist.");
        else if (direction is not null && !direction.IsActive) errors.Add("The selected direction is inactive.");
        if (vehicle is null) errors.Add("The selected vehicle does not exist.");
        else if (vehicle.Status != VehicleStatus.Active) errors.Add("The selected vehicle is not available for dispatch.");
        if (driver is null) errors.Add("The selected driver does not exist.");
        else if (driver.Status != DriverStatus.Active) errors.Add("The selected driver is not available for dispatch.");
        if (bay is null) errors.Add("The selected bay does not exist.");
        else if (bay.Status != BayStatus.Available) errors.Add("The selected bay is not available for dispatch.");

        if (route is not null && route.CentreId != assignment.CentreId) errors.Add("The route does not belong to the selected centre.");
        if (direction is not null && direction.RouteId != assignment.RouteId) errors.Add("The selected direction does not belong to the selected route.");
        if (vehicle is not null && vehicle.CentreId != assignment.CentreId) errors.Add("The vehicle does not belong to the selected centre.");
        if (driver is not null && driver.CentreId != assignment.CentreId) errors.Add("The driver does not belong to the selected centre.");
        if (bay is not null && direction is not null && bay.CentreId != direction.StartCentreId) errors.Add("The bay must belong to the direction's departure centre.");
        else if (bay is not null && direction is null && bay.CentreId != assignment.CentreId) errors.Add("The bay does not belong to the selected centre.");

        // Time clashes are only worth checking once everything else is valid.
        if (route is not null && errors.Count == 0)
        {
            var duration = direction?.EstimatedDurationMin ?? route.EstimatedDurationMin;
            errors.AddRange(await conflicts.FindConflicts(
                assignment.VehicleId, assignment.DriverId, assignment.BayId, assignment.ScheduledTime, duration, assignment.ExcludedTripId, cancellationToken));
        }

        if (errors.Count > 0) throw new BadRequestException(string.Join(" ", errors), errors);
    }

    // Dispatch can make these status changes. Cancelling goes through the refund flow.
    private static bool CanTransition(TripStatus from, TripStatus to) => (from, to) switch
    {
        (TripStatus.Scheduled, TripStatus.Ready or TripStatus.Delayed or TripStatus.Cancelled) => true,
        (TripStatus.Ready, TripStatus.Boarding or TripStatus.Delayed or TripStatus.Cancelled) => true,
        (TripStatus.Boarding, TripStatus.Dispatched or TripStatus.Delayed or TripStatus.Cancelled) => true,
        (TripStatus.Delayed, TripStatus.Ready or TripStatus.Boarding or TripStatus.Dispatched or TripStatus.Cancelled) => true,
        (TripStatus.Dispatched, TripStatus.Delayed or TripStatus.Completed) => true,
        _ => false
    };

    private sealed record Assignment(
        Guid CentreId,
        Guid RouteId,
        Guid? DirectionId,
        Guid VehicleId,
        Guid DriverId,
        Guid BayId,
        DateTime ScheduledTime,
        Guid? ExcludedTripId = null);
}
