using System.Net;
using System.Net.Http.Json;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: fares, payments/refunds and support requests must not be open to anonymous callers or commuters.</summary>
public sealed class FareAndPaymentAccessControlTests
{
    [Fact]
    public async Task Anonymous_CannotCreateAFareRule()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/fare-rules", new { routeId = world.Route.Id, amount = 0.01m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotChangeAFare()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await TestWorld.AddPassengerAsync(factory, world, 0, 100m);
        var ruleId = await factory.WithDbAsync(async db => db.FareRules.Select(r => r.Id).Single());
        using var client = factory.CreateClientAs("Commuter");

        var update = await client.PutAsJsonAsync($"/api/v1/fare-rules/{ruleId}", new { amount = 0.01m, isActive = true });
        var delete = await client.DeleteAsync($"/api/v1/fare-rules/{ruleId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotTriggerAPaymentRefund()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync($"/api/v1/payments/bookings/{Guid.NewGuid()}/refund", new { reason = "free money" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commuter_CannotTriggerAPaymentRefund()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Commuter");

        var response = await client.PostAsJsonAsync($"/api/v1/payments/bookings/{Guid.NewGuid()}/refund", new { reason = "free money" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotStartACheckoutForAnotherPassenger()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/payments/checkout", new { tripId = Guid.NewGuid(), passengerId = Guid.NewGuid(), passengerCount = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotReadOrCreateSupportRequests()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        var list = await client.GetAsync("/api/v1/support-requests");
        var create = await client.PostAsJsonAsync("/api/v1/support-requests", new { type = "Complaint", priority = "Low", subject = "x", description = "y" });

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }
}
