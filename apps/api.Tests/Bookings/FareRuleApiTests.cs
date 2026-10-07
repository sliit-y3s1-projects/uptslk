using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: standard fares per route (one fare rule per route, soft-deactivated).</summary>
public sealed class FareRuleApiTests
{
    private const string Url = "/api/v1/fare-rules";

    [Fact]
    public async Task Create_ForAnActiveRoute_ReturnsCreatedActiveFare()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, new { routeId = world.Route.Id, amount = 250.50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(250.50m, body.GetProperty("amount").GetDecimal());
        Assert.True(body.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Create_ASecondFareForTheSameRoute_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        await client.PostAsJsonAsync(Url, new { routeId = world.Route.Id, amount = 100m });

        var response = await client.PostAsJsonAsync(Url, new { routeId = world.Route.Id, amount = 120m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await factory.CountAsync<FareRule>());
    }

    [Fact]
    public async Task Create_ForUnknownOrArchivedRoute_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db =>
        {
            (await db.Routes.FindAsync(world.Route.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, new { routeId = Guid.NewGuid(), amount = 100m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, new { routeId = world.Route.Id, amount = 100m })).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(1000001)]
    public async Task Create_WithAmountOutsideTheAllowedRange_IsRejected(decimal amount)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, new { routeId = world.Route.Id, amount })).StatusCode);
    }

    [Fact]
    public async Task Update_ChangesAmountAndActiveFlag_AndDeleteDeactivatesWithoutRemoving()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        var id = (await (await client.PostAsJsonAsync(Url, new { routeId = world.Route.Id, amount = 100m })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var update = await client.PutAsJsonAsync($"{Url}/{id}", new { amount = 175m, isActive = true });
        var delete = await client.DeleteAsync($"{Url}/{id}");

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var stored = await factory.WithDbAsync(db => db.FareRules.FindAsync(id).AsTask());
        Assert.Equal(175m, stored!.Amount);
        Assert.False(stored.IsActive);
    }

    [Fact]
    public async Task Update_AndDelete_OfUnknownFare_ReturnNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", new { amount = 10m, isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task List_FiltersByRouteCentreAndActive_AndGetReturnsDetail()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var a = await TestWorld.SeedAsync(factory, "AAA", "AAA-1", "LA");
        var b = await TestWorld.SeedAsync(factory, "BBB", "BBB-1", "LB");
        using var admin = factory.CreateClientAs("Admin");
        var idA = (await (await admin.PostAsJsonAsync(Url, new { routeId = a.Route.Id, amount = 100m })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var idB = (await (await admin.PostAsJsonAsync(Url, new { routeId = b.Route.Id, amount = 200m })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await admin.DeleteAsync($"{Url}/{idB}");
        using var commuter = factory.CreateClientAs("Commuter");

        var byRoute = await commuter.GetFromJsonAsync<JsonElement>($"{Url}?routeId={a.Route.Id}");
        var byCentre = await commuter.GetFromJsonAsync<JsonElement>($"{Url}?centreId={b.Centre.Id}");
        var inactive = await commuter.GetFromJsonAsync<JsonElement>($"{Url}?active=false");
        var detail = await commuter.GetFromJsonAsync<JsonElement>($"{Url}/{idA}");

        Assert.Equal(1, byRoute.GetArrayLength());
        Assert.Equal(idB, byCentre[0].GetProperty("id").GetGuid());
        Assert.Equal(idB, inactive[0].GetProperty("id").GetGuid());
        Assert.Equal(100m, detail.GetProperty("amount").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, (await commuter.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Quote_ForCommuter_ReturnsTheFare_AndRequiresAProfile()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (_, userId) = await TestWorld.AddPassengerAsync(factory, world, 0, fare: 130m);
        using var commuter = factory.CreateClientAs("Commuter", userId);
        using var noProfile = factory.CreateClientAs("Commuter", Guid.NewGuid());

        var quote = await commuter.GetFromJsonAsync<JsonElement>($"{Url}/quote/me?tripId={trip.Id}");
        var missing = await noProfile.GetAsync($"{Url}/quote/me?tripId={trip.Id}");

        Assert.Equal(130m, quote.GetProperty("fare").GetDecimal());
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task Quote_ForUnknownTripOrInactivePassenger_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        await factory.WithDbAsync(async db =>
        {
            (await db.Passengers.FindAsync(passenger.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("CentreManager");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Url}/quote?tripId={Guid.NewGuid()}&passengerId={passenger.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"{Url}/quote?tripId={trip.Id}&passengerId={passenger.Id}")).StatusCode);
    }
}
