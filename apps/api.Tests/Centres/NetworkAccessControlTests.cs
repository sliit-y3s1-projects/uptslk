using System.Net;
using System.Net.Http.Json;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Centres;

/// <summary>
/// Component A: write operations on centres, bays and routes must not be open to anonymous callers
/// or to commuters. Reads stay public so commuters can search the network.
/// </summary>
public sealed class NetworkAccessControlTests
{
    [Fact]
    public async Task Anonymous_CanReadCentresAndRoutes()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/centres")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/routes")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateCentre()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/centres", new { code = "HAX", name = "Hack", city = "X", district = "Y" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCloseCentre()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.DeleteAsync($"/api/v1/centres/{world.Centre.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotArchiveRoute()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Commuter");

        var response = await client.DeleteAsync($"/api/v1/routes/{world.Route.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateBay()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync($"/api/v1/centres/{world.Centre.Id}/bays", new { code = "Z9" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
