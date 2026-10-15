using api.Common.Helpers;
using api.DTO.Drivers;
using api.DTO.Incidents;
using api.DTO.Trips;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class DriverDutyService(
    IDriverRepository drivers,
    ITripRepository trips,
    IIncidentRepository incidents,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    MobileNotificationService notifications) : IDriverDutyService
{
    private const string NoDriverRecord = "This account is not linked to a driver record. Contact your centre manager.";
    private const string TripNotFound = "The assigned trip was not found.";
    private const int MaxSearchDays = 7;

    public async Task<DriverProfileResponse> GetProfileAsync(CancellationToken cancellationToken)
    {
        var driver = await GetCurrentDriverAsync(NoDriverRecord, cancellationToken);
        return DriverProfileResponse.From(driver);
    }

    public async Task<IReadOnlyList<DriverTripResponse>> ListTripsAsync(DriverTripFilter filter, CancellationToken cancellationToken)
    {
        var driver = await GetCurrentDriverAsync(NoDriverRecord, cancellationToken);

        var serviceDate = filter.Date ?? SriLankaTime.Today();
        if (filter.FromDate.HasValue != filter.ToDate.HasValue)
            throw new BadRequestException("Provide both fromDate and toDate.");
        var firstDate = filter.FromDate ?? serviceDate;
        var lastDate = filter.ToDate ?? serviceDate;
        if (lastDate.DayNumber < firstDate.DayNumber || lastDate.DayNumber - firstDate.DayNumber >= MaxSearchDays)
            throw new BadRequestException("Duty searches can cover up to seven days.");

        var duties = await trips.GetDriverDutiesAsync(
            driver.Id, SriLankaTime.DayStartUtc(firstDate), SriLankaTime.DayStartUtc(lastDate.AddDays(1)), cancellationToken);
        return duties.Select(DriverTripResponse.From).ToList();
    }

    public async Task UpdateTripStatusAsync(Guid tripId, UpdateTripStatusRequest request, CancellationToken cancellationToken)
    {
        var driver = await GetCurrentDriverAsync(NoDriverRecord, cancellationToken);
        var trip = await trips.GetForDriverAsync(tripId, driver.Id, track: true, cancellationToken)
            ?? throw new NotFoundException(TripNotFound);
        if (!CanDriverTransition(trip.Status, request.Status))
            throw new BadRequestException($"Cannot change a {trip.Status} trip to {request.Status}.");
        if (request.Status == TripStatus.Delayed && string.IsNullOrWhiteSpace(request.Note))
            throw new BadRequestException("Give a reason when marking a trip delayed.");

        var note = Text.CleanOptional(request.Note);
        trip.Status = request.Status;
        trip.Notes = note ?? trip.Notes;
        if (request.Status == TripStatus.Dispatched) trip.ActualDepartureAt = DateTime.UtcNow;
        if (request.Status == TripStatus.Completed) trip.CompletedAt = DateTime.UtcNow;
        trip.UpdatedAt = DateTime.UtcNow;
        await notifications.TripStatusChangedAsync(trip, note, changedByDriver: true, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DriverIncidentCreatedResponse> ReportIncidentAsync(Guid tripId, ReportDriverIncidentRequest request, CancellationToken cancellationToken)
    {
        var driver = await GetCurrentDriverAsync("This account is not linked to an active driver record.", cancellationToken);
        var trip = await trips.GetForDriverAsync(tripId, driver.Id, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(TripNotFound);
        if (trip.Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new BadRequestException("Incidents cannot be reported for completed or cancelled trips.");
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.Severity))
            throw new BadRequestException("Choose a valid incident type and severity.");
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            throw new BadRequestException("Enter an incident title and description.");

        var incident = new Incident
        {
            CentreId = trip.CentreId,
            TripId = trip.Id,
            ReportedById = driver.UserId,
            ReportedByName = driver.FullName,
            Type = request.Type,
            Severity = request.Severity,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            SlaDueAt = DateTime.UtcNow.AddHours(4)
        };
        incidents.Add(incident);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DriverIncidentCreatedResponse(incident.Id, incident.TripId, incident.Status);
    }

    private async Task<Driver> GetCurrentDriverAsync(string notFoundMessage, CancellationToken cancellationToken)
    {
        var driver = currentUser.UserId is { } userId
            ? await drivers.GetActiveByUserIdAsync(userId, cancellationToken)
            : null;
        return driver ?? throw new NotFoundException(notFoundMessage);
    }

    // The status changes a driver may make. Dispatch staff can make any change.
    private static bool CanDriverTransition(TripStatus from, TripStatus to) => (from, to) switch
    {
        (TripStatus.Scheduled, TripStatus.Ready or TripStatus.Delayed) => true,
        (TripStatus.Ready, TripStatus.Boarding or TripStatus.Delayed) => true,
        (TripStatus.Boarding, TripStatus.Dispatched or TripStatus.Delayed) => true,
        (TripStatus.Delayed, TripStatus.Ready or TripStatus.Boarding or TripStatus.Dispatched) => true,
        (TripStatus.Dispatched, TripStatus.Completed or TripStatus.Delayed) => true,
        _ => false
    };
}
