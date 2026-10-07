using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Fleet;

/// <summary>Component B (Fleet and Maintenance): vehicle CRUD, validation, filters and deactivation.</summary>
public sealed class VehicleCrudTests
{
    private const string Url = "/api/v1/vehicles";

    private static object ValidVehicle(Guid centreId, string plate = "ab-1234", int capacity = 52) => new
    {
        centreId, plateNumber = plate, model = "Ashok Leyland", type = "SemiLuxury", capacity, isAccessible = true, status = "Active"
    };

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndNormalizesPlate()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, ValidVehicle(world.Centre.Id, "  ab-1234 "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AB-1234", body.GetProperty("plateNumber").GetString());
    }

    [Fact]
    public async Task Create_WithDuplicatePlate_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, ValidVehicle(world.Centre.Id, world.Vehicle.PlateNumber.ToLower()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await factory.CountAsync<Vehicle>());
    }

    [Fact]
    public async Task Create_ForUnknownCentre_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidVehicle(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(201)]
    public async Task Create_WithCapacityOutsideOneTo200_ReturnsBadRequest(int capacity)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, ValidVehicle(world.Centre.Id, "CAP-1", capacity));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Model")]
    [InlineData("AB-1", "")]
    public async Task Create_WithMissingPlateOrModel_ReturnsBadRequest(string plate, string model)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, new { centreId = world.Centre.Id, plateNumber = plate, model, capacity = 40 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsProfileWithMaintenanceHistory()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            db.Add(new MaintenanceRecord { VehicleId = world.Vehicle.Id, Type = "Service", Description = "Oil change", ScheduledFor = DateTime.UtcNow.AddDays(10) });
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.GetAsync($"{Url}/{world.Vehicle.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(world.Vehicle.PlateNumber, body.GetProperty("plateNumber").GetString());
        Assert.Equal(1, body.GetProperty("maintenance").GetArrayLength());
    }

    [Fact]
    public async Task Get_UnknownVehicle_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("FleetOfficer");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task List_FiltersByCentre_Status_AndSearch()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var a = await TestWorld.SeedAsync(factory, "AAA", "AAA-1111", "LA");
        var b = await TestWorld.SeedAsync(factory, "BBB", "BBB-2222", "LB");
        await factory.WithDbAsync(async db =>
        {
            var vehicle = await db.Vehicles.FindAsync(b.Vehicle.Id);
            vehicle!.Status = VehicleStatus.Maintenance;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("FleetOfficer");

        var byCentre = await client.GetFromJsonAsync<JsonElement>($"{Url}?centreId={a.Centre.Id}");
        var byStatus = await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Maintenance");
        var bySearch = await client.GetFromJsonAsync<JsonElement>($"{Url}?search=bbb");

        Assert.Equal(1, byCentre.GetArrayLength());
        Assert.Equal("AAA-1111", byCentre[0].GetProperty("plateNumber").GetString());
        Assert.Equal("BBB-2222", byStatus[0].GetProperty("plateNumber").GetString());
        Assert.Equal(1, bySearch.GetArrayLength());
    }

    [Fact]
    public async Task Update_ChangesStatusCapacityAndAccessibility()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{world.Vehicle.Id}", new
        {
            centreId = world.Centre.Id, plateNumber = world.Vehicle.PlateNumber, model = "Tata", type = "AcExpress",
            capacity = 30, isAccessible = true, status = "Maintenance"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Equal(30, stored!.Capacity);
        Assert.True(stored.IsAccessible);
        Assert.Equal(VehicleStatus.Maintenance, stored.Status);
    }

    [Fact]
    public async Task Update_ToPlateUsedByAnotherVehicle_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "OTHER-1");
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{other.Id}", new
        {
            centreId = world.Centre.Id, plateNumber = world.Vehicle.PlateNumber, model = "Bus", capacity = 40
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DeactivatesVehicle_AndRetainsHistory()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.DeleteAsync($"{Url}/{world.Vehicle.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Equal(VehicleStatus.Inactive, stored!.Status);
        Assert.Equal(1, await factory.CountAsync<Trip>());
    }

    [Fact]
    public async Task UploadImage_AsCommuter_ReturnsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Commuter");
        using var form = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "file", "bus.png" } };

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
