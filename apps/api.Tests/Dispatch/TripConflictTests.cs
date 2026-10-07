using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C: conflict detection for vehicles, drivers and bays when creating or editing trips.</summary>
public sealed class TripConflictTests
{
    private const string Url = "/api/v1/trips";

    private static async Task<string[]> ErrorsAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors")
            .EnumerateArray().Select(error => error.GetString()!).ToArray();

    [Fact]
    public async Task SameVehicleAtOverlappingTime_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world);
        var otherDriver = await TestWorld.AddDriverAsync(factory, world.Centre.Id, "L-2");
        var otherBay = await TestWorld.AddBayAsync(factory, world.Centre.Id, "B-2");
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(
            world, TestWorld.FutureDeparture.AddMinutes(30), driverId: otherDriver.Id, bayId: otherBay.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(await ErrorsAsync(response), error => error.Contains("Vehicle is already assigned"));
    }

    [Fact]
    public async Task SameDriverAtOverlappingTime_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world);
        var otherVehicle = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "V-2");
        var otherBay = await TestWorld.AddBayAsync(factory, world.Centre.Id, "B-2");
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(
            world, TestWorld.FutureDeparture.AddMinutes(30), vehicleId: otherVehicle.Id, bayId: otherBay.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(await ErrorsAsync(response), error => error.Contains("Driver is already assigned"));
    }

    [Fact]
    public async Task SameBayWithinTenMinutes_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world);
        var otherVehicle = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "V-2");
        var otherDriver = await TestWorld.AddDriverAsync(factory, world.Centre.Id, "L-2");
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(
            world, TestWorld.FutureDeparture.AddMinutes(5), vehicleId: otherVehicle.Id, driverId: otherDriver.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(await ErrorsAsync(response), error => error.Contains("Bay is already assigned"));
    }

    [Fact]
    public async Task SameBayAfterTheBoardingWindow_IsAllowed()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world);
        var otherVehicle = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "V-2");
        var otherDriver = await TestWorld.AddDriverAsync(factory, world.Centre.Id, "L-2");
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(
            world, TestWorld.FutureDeparture.AddMinutes(15), vehicleId: otherVehicle.Id, driverId: otherDriver.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SameVehicleAfterThePreviousJourneyEnds_IsAllowed()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world); // 60-minute route
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(world, TestWorld.FutureDeparture.AddMinutes(61)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, await factory.CountAsync<Trip>());
    }

    [Fact]
    public async Task EditingATripDoesNotConflictWithItself()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{trip.Id}", TripCrudTests.TripRequest(world, trip.ScheduledTime.AddMinutes(10)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CancelledTripNoLongerBlocksTheVehicle()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world, status: api.Enums.TripStatus.Cancelled);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(world));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task VehicleWithScheduledMaintenanceThatDay_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            db.Add(new MaintenanceRecord { VehicleId = world.Vehicle.Id, Type = "Service", Description = "Brakes", ScheduledFor = TestWorld.FutureDeparture });
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripCrudTests.TripRequest(world));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(await ErrorsAsync(response), error => error.Contains("maintenance"));
    }
}
