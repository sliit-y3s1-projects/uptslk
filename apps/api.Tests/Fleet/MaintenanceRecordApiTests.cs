using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Fleet;

/// <summary>Component B (Fleet and Maintenance): maintenance records, authorization and trip-conflict protection.</summary>
public sealed class MaintenanceRecordApiTests
{
    private const string Url = "/api/v1/maintenance-records";

    private static object ValidRecord(Guid vehicleId, DateTime? scheduledFor = null) => new
    {
        vehicleId, type = "Service", description = "Routine 10,000 km service",
        scheduledFor = scheduledFor ?? DateTime.UtcNow.AddDays(30)
    };

    [Fact]
    public async Task Create_AsFleetOfficerOfSameCentre_ReturnsCreatedScheduledRecord()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, ValidRecord(world.Vehicle.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Scheduled", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_WithoutLogin_ReturnsUnauthorized()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(Url, ValidRecord(world.Vehicle.Id));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsCommuter_ReturnsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Commuter");

        var response = await client.PostAsJsonAsync(Url, ValidRecord(world.Vehicle.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsManagerOfAnotherCentre_ReturnsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("CentreManager", centreId: Guid.NewGuid());

        var response = await client.PostAsJsonAsync(Url, ValidRecord(world.Vehicle.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await factory.CountAsync<MaintenanceRecord>());
    }

    [Fact]
    public async Task Create_ForUnknownVehicle_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidRecord(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Service", "")]
    public async Task Create_WithMissingTypeOrDescription_ReturnsBadRequest(string type, string description)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, new { vehicleId = world.Vehicle.Id, type, description, scheduledFor = DateTime.UtcNow.AddDays(5) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_OnDayWhenVehicleHasAnActiveTrip_ReturnsConflictWithTripIds()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidRecord(world.Vehicle.Id, trip.ScheduledTime));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(trip.Id, body.GetProperty("conflictingTripIds")[0].GetGuid());
    }

    [Fact]
    public async Task Update_ToCompletedWithoutCompletionTime_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var recordId = await SeedRecordAsync(factory, world);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/{recordId}", new
        {
            type = "Service", description = "Done", status = "Completed", scheduledFor = DateTime.UtcNow.AddDays(30)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ToCompletedWithCompletionTime_Succeeds()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var recordId = await SeedRecordAsync(factory, world);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/{recordId}", new
        {
            type = "Service", description = "Done", status = "Completed",
            scheduledFor = DateTime.UtcNow.AddDays(30), completedAt = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.MaintenanceRecords.FindAsync(recordId).AsTask());
        Assert.Equal(MaintenanceStatus.Completed, stored!.Status);
        Assert.NotNull(stored.CompletedAt);
    }

    [Fact]
    public async Task Delete_CancelsRecord_ButKeepsIt()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var recordId = await SeedRecordAsync(factory, world);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.DeleteAsync($"{Url}/{recordId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.MaintenanceRecords.FindAsync(recordId).AsTask());
        Assert.Equal(MaintenanceStatus.Cancelled, stored!.Status);
    }

    [Fact]
    public async Task List_FiltersByVehicleAndStatus()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "OTHER-9");
        await SeedRecordAsync(factory, world);
        await factory.WithDbAsync(async db =>
        {
            db.Add(new MaintenanceRecord { VehicleId = other.Id, Type = "Inspection", Description = "Annual", Status = MaintenanceStatus.Completed, ScheduledFor = DateTime.UtcNow, CompletedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Admin");

        var byVehicle = await client.GetFromJsonAsync<JsonElement>($"{Url}?vehicleId={world.Vehicle.Id}");
        var completed = await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Completed");

        Assert.Equal(1, byVehicle.GetArrayLength());
        Assert.Equal(1, completed.GetArrayLength());
        Assert.Equal("OTHER-9", completed[0].GetProperty("vehicle").GetString());
    }

    [Fact]
    public async Task Get_UnknownRecord_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }

    private static Task<Guid> SeedRecordAsync(TestApiFactory factory, TestWorld world) =>
        factory.WithDbAsync(async db =>
        {
            var record = new MaintenanceRecord { VehicleId = world.Vehicle.Id, Type = "Service", Description = "Routine", ScheduledFor = DateTime.UtcNow.AddDays(30) };
            db.Add(record);
            await db.SaveChangesAsync();
            return record.Id;
        });
}
