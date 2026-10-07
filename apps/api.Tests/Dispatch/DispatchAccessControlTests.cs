using System.Net;
using System.Net.Http.Json;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C: trips can only be created, changed or cancelled by authenticated dispatch staff.</summary>
public sealed class DispatchAccessControlTests
{
    [Fact]
    public async Task Anonymous_CannotCreateTrip()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/trips", TripCrudTests.TripRequest(world));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotChangeTripStatus()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsJsonAsync($"/api/v1/trips/{trip.Id}/status", new { status = "Ready" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotCancelTrip()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Commuter");

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{trip.Id}")
        {
            Content = JsonContent.Create(new { reason = "just because" })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Driver_CannotListIncidentsOfOtherTrips_ThroughGeneralApi()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Driver");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/incidents")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotReadDriverDuties()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/drivers/me/trips")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotListDriversOrSeeTheirLicenseNumbers()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/drivers")).StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotReadADriversProfile()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Commuter");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/drivers/{world.Driver.Id}")).StatusCode);
    }
}
