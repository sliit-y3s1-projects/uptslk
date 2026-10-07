using api.Data;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Route = api.Models.Route;

namespace api.Tests.Shared;

/// <summary>One operating centre with the resources a trip needs. Built directly in the database.</summary>
internal sealed record TestWorld(Centre Centre, Route Route, Vehicle Vehicle, Driver Driver, Bay Bay)
{
    public static readonly DateTime FutureDeparture = DateTime.UtcNow.Date.AddDays(3).AddHours(4);

    public static async Task<TestWorld> SeedAsync(
        WebApplicationFactory<Program> factory, string code = "CMB", string plate = "NB-1000", string license = "LIC-1000")
    {
        return await factory.WithDbAsync(async db =>
        {
            var world = Build(code, plate, license);
            db.Add(world.Centre);
            await db.SaveChangesAsync();
            db.AddRange(world.Route, world.Vehicle, world.Bay);
            await db.SaveChangesAsync();
            db.Add(world.Driver);
            await db.SaveChangesAsync();
            return world;
        });
    }

    private static TestWorld Build(string code, string plate, string license)
    {
        var centre = new Centre
        {
            Code = code, Name = $"{code} Centre", City = "Colombo", District = "Colombo",
            Status = CentreStatus.Operating
        };
        var route = new Route
        {
            CentreId = centre.Id, RouteNumber = $"R-{code}", Name = "Test Route", Origin = "A", Destination = "B",
            DistanceKm = 50, EstimatedDurationMin = 60
        };
        var vehicle = new Vehicle { CentreId = centre.Id, PlateNumber = plate, Model = "Bus", Capacity = 40 };
        var driver = new Driver { CentreId = centre.Id, FullName = "Test Driver", LicenseNumber = license };
        var bay = new Bay { CentreId = centre.Id, Code = $"B-{code}" };
        return new TestWorld(centre, route, vehicle, driver, bay);
    }

    public static async Task<Vehicle> AddVehicleAsync(WebApplicationFactory<Program> factory, Guid centreId, string plate, int capacity = 40) =>
        await factory.WithDbAsync(async db =>
        {
            var vehicle = new Vehicle { CentreId = centreId, PlateNumber = plate, Model = "Bus", Capacity = capacity };
            db.Add(vehicle);
            await db.SaveChangesAsync();
            return vehicle;
        });

    public static async Task<Driver> AddDriverAsync(WebApplicationFactory<Program> factory, Guid centreId, string license) =>
        await factory.WithDbAsync(async db =>
        {
            var driver = new Driver { CentreId = centreId, FullName = $"Driver {license}", LicenseNumber = license };
            db.Add(driver);
            await db.SaveChangesAsync();
            return driver;
        });

    public static async Task<Bay> AddBayAsync(WebApplicationFactory<Program> factory, Guid centreId, string code) =>
        await factory.WithDbAsync(async db =>
        {
            var bay = new Bay { CentreId = centreId, Code = code };
            db.Add(bay);
            await db.SaveChangesAsync();
            return bay;
        });

    public static async Task<Trip> AddTripAsync(
        WebApplicationFactory<Program> factory, TestWorld world, DateTime? scheduledTime = null, TripStatus status = TripStatus.Scheduled) =>
        await factory.WithDbAsync(async db =>
        {
            var trip = new Trip
            {
                CentreId = world.Centre.Id, RouteId = world.Route.Id, VehicleId = world.Vehicle.Id,
                DriverId = world.Driver.Id, BayId = world.Bay.Id,
                ScheduledTime = scheduledTime ?? FutureDeparture, Status = status
            };
            db.Add(trip);
            await db.SaveChangesAsync();
            return trip;
        });

    /// <summary>A commuter with an active passenger profile, a wallet and an active standard fare on the route.</summary>
    public static async Task<(Passenger Passenger, Guid UserId)> AddPassengerAsync(
        WebApplicationFactory<Program> factory, TestWorld world, decimal balance, decimal fare = 100m, bool addFareRule = true) =>
        await factory.WithDbAsync(async db =>
        {
            var user = new User
            {
                UserName = $"p{Guid.NewGuid():N}@test.local", Email = $"p{Guid.NewGuid():N}@test.local",
                Name = "Test Passenger", Role = UserRole.Commuter
            };
            db.Add(user);
            await db.SaveChangesAsync();
            var passenger = new Passenger
            {
                UserId = user.Id, FullName = "Test Passenger", PhoneNumber = $"07{Random.Shared.NextInt64(10000000, 99999999)}", Category = PassengerCategory.Adult
            };
            db.Add(passenger);
            await db.SaveChangesAsync();
            db.Add(new Wallet { PassengerId = passenger.Id, Balance = balance });
            if (addFareRule && !db.FareRules.Any(rule => rule.RouteId == world.Route.Id))
                db.Add(new FareRule { RouteId = world.Route.Id, Amount = fare });
            await db.SaveChangesAsync();
            return (passenger, user.Id);
        });
}
