using System.Net;
using System.Net.Http.Json;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: passenger data and money-moving endpoints must require authentication and a staff role.</summary>
public sealed class PassengerAccessControlTests
{
    [Fact]
    public async Task Anonymous_CannotListPassengers()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/passengers")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotTopUpAWallet()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync($"/api/v1/passengers/{passenger.Id}/wallet/top-ups", new { amount = 100000 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateABookingOnSomeoneElsesWallet()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 500);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/bookings", new { tripId = trip.Id, passengerId = passenger.Id });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotReadAnotherPassengersProfile()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (victim, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        using var client = factory.CreateClientAs("Commuter");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/passengers/{victim.Id}")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCancelSomeonesBooking()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new { reason = "x" })
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
