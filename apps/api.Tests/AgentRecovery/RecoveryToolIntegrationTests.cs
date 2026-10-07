using api.Enums;
using api.Models;
using api.Services;
using api.Services.AgentRecovery.Tools;
using Microsoft.EntityFrameworkCore;
using Xunit;

using api.Tests.Shared;
namespace api.Tests.AgentRecovery;

public sealed class RecoveryToolIntegrationTests
{
    [Fact]
    public async Task NetworkTool_ReturnsFirstAvailableAlternativeBayAtDepartureCentre()
    {
        await using var database = await TestDb.CreateAsync();
        var centre = CreateCentre("KAD");
        var excluded = CreateBay(centre, "B01", BayStatus.Available);
        var unavailable = CreateBay(centre, "B02", BayStatus.Occupied);
        var expected = CreateBay(centre, "B03", BayStatus.Available);
        database.Context.AddRange(centre, excluded, unavailable, expected);
        await database.Context.SaveChangesAsync();

        var result = await new NetworkRecoveryTools(database.Context).FindDepartureBayAsync(
            new FindDepartureBayInput(centre.Id, excluded.Id),
            CancellationToken.None);

        Assert.Equal(expected.Id, result.CandidateBayId);
        Assert.Equal("B03", result.CandidateBayCode);
        Assert.Equal(1, result.CandidatesChecked);
    }

    [Fact]
    public async Task FleetTool_ExcludesVehiclesWithActiveMaintenanceAndSelectsSufficientCapacity()
    {
        await using var database = await TestDb.CreateAsync();
        var centre = CreateCentre("KAD");
        var excluded = CreateVehicle(centre, "CURRENT", 45);
        var maintained = CreateVehicle(centre, "MAINTAINED", 55);
        var expected = CreateVehicle(centre, "READY", 50);
        maintained.MaintenanceRecords.Add(new MaintenanceRecord
        {
            VehicleId = maintained.Id,
            Vehicle = maintained,
            Type = "Engine repair",
            Description = "Not available for service",
            Status = MaintenanceStatus.InProgress,
            ScheduledFor = DateTime.UtcNow
        });
        database.Context.AddRange(centre, excluded, maintained, expected);
        await database.Context.SaveChangesAsync();

        var result = await new FleetRecoveryTools(database.Context).FindReplacementVehicleAsync(
            new FindReplacementVehicleInput(centre.Id, excluded.Id, 48, DateTime.UtcNow.AddHours(2), 60),
            CancellationToken.None);

        Assert.Equal(expected.Id, result.CandidateVehicleId);
        Assert.True(result.MeetsRequiredCapacity);
        Assert.Equal(1, result.CandidatesChecked);
    }

    [Fact]
    public async Task FleetTool_ExcludesVehiclesReservedForMaintenanceOnTripDate()
    {
        await using var database = await TestDb.CreateAsync();
        var centre = CreateCentre("KAD");
        var excluded = CreateVehicle(centre, "CURRENT", 45);
        var reserved = CreateVehicle(centre, "RESERVED", 55);
        var available = CreateVehicle(centre, "AVAILABLE", 50);
        reserved.MaintenanceRecords.Add(new MaintenanceRecord
        {
            VehicleId = reserved.Id,
            Vehicle = reserved,
            Type = "Brake service",
            Description = "Reserved for the service day",
            Status = MaintenanceStatus.Scheduled,
            ScheduledFor = new DateTime(2026, 10, 1, 3, 30, 0, DateTimeKind.Utc)
        });
        database.Context.AddRange(centre, excluded, reserved, available);
        await database.Context.SaveChangesAsync();

        var result = await new FleetRecoveryTools(database.Context).FindReplacementVehicleAsync(
            new FindReplacementVehicleInput(centre.Id, excluded.Id, 48,
                new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc), 60),
            CancellationToken.None);

        Assert.Equal(available.Id, result.CandidateVehicleId);
        Assert.Equal(1, result.CandidatesChecked);
    }

    [Fact]
    public async Task DispatchTool_SkipsConflictingDriverAndReturnsNextAvailableDriver()
    {
        await using var database = await TestDb.CreateAsync();
        var centre = CreateCentre("KAD");
        var proposedVehicle = CreateVehicle(centre, "PROPOSED", 50);
        var occupiedVehicle = CreateVehicle(centre, "OCCUPIED", 45);
        var proposedBay = CreateBay(centre, "B01", BayStatus.Available);
        var occupiedBay = CreateBay(centre, "B02", BayStatus.Available);
        var excludedDriver = CreateDriver(centre, "Affected Driver", "LIC-01");
        var conflictingDriver = CreateDriver(centre, "Alpha Driver", "LIC-02");
        var expectedDriver = CreateDriver(centre, "Beta Driver", "LIC-03");
        var route = CreateRoute(centre);
        var requestedTime = DateTime.UtcNow.AddHours(2);
        var conflictingTrip = CreateTrip(
            centre,
            route,
            occupiedVehicle,
            conflictingDriver,
            occupiedBay,
            requestedTime);
        var excludedTripId = Guid.NewGuid();
        database.Context.AddRange(
            centre,
            proposedVehicle,
            occupiedVehicle,
            proposedBay,
            occupiedBay,
            excludedDriver,
            conflictingDriver,
            expectedDriver,
            route,
            conflictingTrip);
        await database.Context.SaveChangesAsync();

        var tools = new DispatchRecoveryTools(database.Context, new TripConflictService(database.Context));
        var result = await tools.FindConflictFreeDriverAsync(
            new FindConflictFreeDriverInput(
                centre.Id,
                excludedDriver.Id,
                proposedVehicle.Id,
                proposedBay.Id,
                requestedTime,
                route.EstimatedDurationMin,
                excludedTripId),
            CancellationToken.None);

        Assert.Equal(expectedDriver.Id, result.CandidateDriverId);
        Assert.Equal("Beta Driver", result.DriverName);
        Assert.Equal(2, result.CandidatesChecked);
    }

    [Fact]
    public async Task PassengerTool_CountsOnlyActiveBookings()
    {
        await using var database = await TestDb.CreateAsync();
        var scenario = CreateTripScenario();
        database.Context.AddRange(
            scenario.Centre,
            scenario.Route,
            scenario.Vehicle,
            scenario.Driver,
            scenario.Bay,
            scenario.Trip);
        database.Context.Bookings.AddRange(
            CreateBooking(scenario.Trip, "A1", 2, BookingStatus.Pending),
            CreateBooking(scenario.Trip, "A2", 3, BookingStatus.Confirmed),
            CreateBooking(scenario.Trip, "A3", 4, BookingStatus.Cancelled));
        await database.Context.SaveChangesAsync();

        var result = await new PassengerRecoveryTools(database.Context).AssessPassengerImpactAsync(
            new AssessPassengerImpactInput(scenario.Trip.Id),
            CancellationToken.None);

        Assert.Equal(5, result.AffectedPassengers);
        Assert.True(result.NotificationRecommended);
        Assert.False(result.RefundRecommended);
    }

    internal static (Centre Centre, Route Route, Vehicle Vehicle, Driver Driver, Bay Bay, Trip Trip) CreateTripScenario()
    {
        var centre = CreateCentre("KAD");
        var route = CreateRoute(centre);
        var vehicle = CreateVehicle(centre, "CURRENT", 45);
        var driver = CreateDriver(centre, "Current Driver", "LIC-CURRENT");
        var bay = CreateBay(centre, "B01", BayStatus.Available);
        var trip = CreateTrip(centre, route, vehicle, driver, bay, DateTime.UtcNow.AddHours(1));
        return (centre, route, vehicle, driver, bay, trip);
    }

    internal static Centre CreateCentre(string code) => new()
    {
        Code = code,
        Name = $"{code} Centre",
        City = "Colombo",
        District = "Colombo",
        Status = CentreStatus.Operating
    };

    internal static Bay CreateBay(Centre centre, string code, BayStatus status) => new()
    {
        CentreId = centre.Id,
        Centre = centre,
        Code = code,
        Name = code,
        Status = status
    };

    internal static Vehicle CreateVehicle(Centre centre, string plate, int capacity) => new()
    {
        CentreId = centre.Id,
        Centre = centre,
        PlateNumber = plate,
        Model = "Test bus",
        Type = VehicleType.Normal,
        Capacity = capacity,
        Status = VehicleStatus.Active
    };

    internal static Driver CreateDriver(Centre centre, string name, string license) => new()
    {
        CentreId = centre.Id,
        Centre = centre,
        FullName = name,
        LicenseNumber = license,
        Status = DriverStatus.Active
    };

    internal static Route CreateRoute(Centre centre) => new()
    {
        CentreId = centre.Id,
        Centre = centre,
        RouteNumber = $"R-{centre.Code}",
        Name = "Test route",
        Origin = "Origin",
        Destination = "Destination",
        ServiceType = VehicleType.Normal,
        DistanceKm = 25,
        EstimatedDurationMin = 60
    };

    internal static Trip CreateTrip(
        Centre centre,
        Route route,
        Vehicle vehicle,
        Driver driver,
        Bay bay,
        DateTime scheduledTime) => new()
    {
        CentreId = centre.Id,
        Centre = centre,
        RouteId = route.Id,
        Route = route,
        VehicleId = vehicle.Id,
        Vehicle = vehicle,
        DriverId = driver.Id,
        Driver = driver,
        BayId = bay.Id,
        Bay = bay,
        ScheduledTime = scheduledTime,
        Status = TripStatus.Scheduled
    };

    internal static Booking CreateBooking(
        Trip trip,
        string seat,
        int passengerCount,
        BookingStatus status)
    {
        var passenger = new Passenger
        {
            FullName = $"Passenger {seat}",
            PhoneNumber = $"077{Guid.NewGuid():N}"[..10],
            Category = PassengerCategory.Adult
        };
        return new Booking
        {
            TripId = trip.Id,
            Trip = trip,
            PassengerId = passenger.Id,
            Passenger = passenger,
            SeatNumber = seat,
            PassengerCount = passengerCount,
            Fare = 100,
            PassengerCategory = PassengerCategory.Adult,
            QrCode = Guid.NewGuid().ToString("N"),
            Status = status
        };
    }
}
