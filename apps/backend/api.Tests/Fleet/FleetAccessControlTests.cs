using System.Net;
using System.Net.Http.Json;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Fleet;

/// <summary>Component B: only fleet staff may change vehicles; anonymous users and commuters may not.</summary>
public sealed class FleetAccessControlTests
{
    [Fact]
    public async Task Anonymous_CannotCreateVehicle()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            centreId = world.Centre.Id, plateNumber = "HAX-1", model = "Bus", capacity = 40
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotDeactivateVehicle()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.DeleteAsync($"/api/v1/vehicles/{world.Vehicle.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotUpdateVehicle()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Commuter");

        var response = await client.PutAsJsonAsync($"/api/v1/vehicles/{world.Vehicle.Id}", new
        {
            centreId = world.Centre.Id, plateNumber = world.Vehicle.PlateNumber, model = "Bus", capacity = 40
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
