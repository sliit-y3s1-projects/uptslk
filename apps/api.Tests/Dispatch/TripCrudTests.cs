using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C (Scheduling and Dispatch): trip create/read/update/cancel and assignment validation.</summary>
public sealed class TripCrudTests
{
    private const string Url = "/api/v1/trips";

    internal static object TripRequest(TestWorld w, DateTime? at = null, Guid? vehicleId = null, Guid? driverId = null, Guid? bayId = null) => new
    {
        centreId = w.Centre.Id, routeId = w.Route.Id, vehicleId = vehicleId ?? w.Vehicle.Id,
        driverId = driverId ?? w.Driver.Id, bayId = bayId ?? w.Bay.Id,
        scheduledTime = at ?? TestWorld.FutureDeparture, notes = "  first run "
    };

    [Fact]
    public async Task Create_WithValidAssignment_ReturnsCreatedScheduledTrip()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripRequest(world));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Scheduled", body.GetProperty("status").GetString());
        var stored = await factory.WithDbAsync(db => db.Trips.FindAsync(body.GetProperty("id").GetGuid()).AsTask());
        Assert.Equal("first run", stored!.Notes);
    }

    [Fact]
    public async Task Create_WithVehicleUnderMaintenance_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            (await db.Vehicles.FindAsync(world.Vehicle.Id))!.Status = VehicleStatus.Maintenance;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripRequest(world));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountAsync<Trip>());
    }

    [Fact]
    public async Task Create_WithInactiveDriver_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            (await db.Drivers.FindAsync(world.Driver.Id))!.Status = DriverStatus.Inactive;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, TripRequest(world))).StatusCode);
    }

    [Fact]
    public async Task Create_WithBayOutOfService_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            (await db.Bays.FindAsync(world.Bay.Id))!.Status = BayStatus.OutOfService;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, TripRequest(world))).StatusCode);
    }

    [Fact]
    public async Task Create_WithArchivedRoute_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            (await db.Routes.FindAsync(world.Route.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, TripRequest(world))).StatusCode);
    }

    [Fact]
    public async Task Create_AtCentreThatIsNotOperating_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            (await db.Centres.FindAsync(world.Centre.Id))!.Status = CentreStatus.Suspended;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, TripRequest(world))).StatusCode);
    }

    [Fact]
    public async Task Create_WithVehicleFromAnotherCentre_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTH", "OTH-1", "LO");
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripRequest(world, vehicleId: other.Vehicle.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownResources_ReturnsAllValidationErrors()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, TripRequest(world, vehicleId: Guid.NewGuid(), driverId: Guid.NewGuid(), bayId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Get_ReturnsDetailWithAssignments()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.GetAsync($"{Url}/{trip.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(world.Vehicle.PlateNumber, body.GetProperty("vehicle").GetString());
        Assert.Equal(world.Bay.Code, body.GetProperty("bay").GetString());
    }

    [Fact]
    public async Task Get_UnknownTrip_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Dispatcher");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatus_AndByDate()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var day = TestWorld.FutureDeparture;
        await TestWorld.AddTripAsync(factory, world, day, TripStatus.Scheduled);
        await TestWorld.AddTripAsync(factory, world, day.AddHours(3), TripStatus.Delayed);
        await TestWorld.AddTripAsync(factory, world, day.AddDays(5), TripStatus.Scheduled);
        using var client = factory.CreateClientAs("Dispatcher");

        var delayed = await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Delayed");
        var onDay = await client.GetFromJsonAsync<JsonElement>($"{Url}?date={day:yyyy-MM-dd}");
        var all = await client.GetFromJsonAsync<JsonElement>(Url);

        Assert.Equal(1, delayed.GetArrayLength());
        Assert.Equal(2, onDay.GetArrayLength());
        Assert.Equal(3, all.GetArrayLength());
    }

    [Fact]
    public async Task History_ShowsOnlyCompletedAndCancelledTrips()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world, status: TripStatus.Scheduled);
        await TestWorld.AddTripAsync(factory, world, TestWorld.FutureDeparture.AddHours(5), TripStatus.Completed);
        await TestWorld.AddTripAsync(factory, world, TestWorld.FutureDeparture.AddHours(9), TripStatus.Cancelled);
        using var client = factory.CreateClientAs("Dispatcher");

        var history = await client.GetFromJsonAsync<JsonElement>($"{Url}/history");

        Assert.Equal(2, history.GetArrayLength());
    }

    [Fact]
    public async Task Update_ChangesDepartureTime_AndKeepsOtherAssignments()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var newTime = TestWorld.FutureDeparture.AddHours(6);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{trip.Id}", TripRequest(world, newTime));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Trips.FindAsync(trip.Id).AsTask());
        Assert.Equal(newTime, stored!.ScheduledTime);
        Assert.Equal(world.Vehicle.Id, stored.VehicleId);
    }

    [Theory]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Cancelled)]
    public async Task Update_OfFinishedTrip_IsRejected(TripStatus status)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world, status: status);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{trip.Id}", TripRequest(world));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reassign_SwapsVehicleAndDriver()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var spareVehicle = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "SPARE-1");
        var spareDriver = await TestWorld.AddDriverAsync(factory, world.Centre.Id, "SPARE-LIC");
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync($"{Url}/{trip.Id}/reassign", new
        {
            vehicleId = spareVehicle.Id, driverId = spareDriver.Id, bayId = world.Bay.Id, scheduledTime = trip.ScheduledTime
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Trips.FindAsync(trip.Id).AsTask());
        Assert.Equal(spareVehicle.Id, stored!.VehicleId);
        Assert.Equal(spareDriver.Id, stored.DriverId);
    }

    [Fact]
    public async Task Cancel_WithoutReason_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{trip.Id}")
        {
            Content = JsonContent.Create(new { reason = "" })
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WithReason_CancelsTrip_ButKeepsTheRecord()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{trip.Id}")
        {
            Content = JsonContent.Create(new { reason = "  Road closed " })
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Trips.FindAsync(trip.Id).AsTask());
        Assert.Equal(TripStatus.Cancelled, stored!.Status);
        Assert.Equal("Road closed", stored.CancellationReason);
    }

    [Fact]
    public async Task Cancel_AnAlreadyCancelledTrip_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world, status: TripStatus.Cancelled);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{trip.Id}")
        {
            Content = JsonContent.Create(new { reason = "again" })
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
