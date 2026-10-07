using System.Net;
using System.Net.Http.Json;
using api.Enums;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C: dispatch state machine for PATCH /trips/{id}/status.</summary>
public sealed class TripLifecycleTests
{
    [Theory]
    [InlineData(TripStatus.Scheduled, TripStatus.Ready)]
    [InlineData(TripStatus.Scheduled, TripStatus.Delayed)]
    [InlineData(TripStatus.Ready, TripStatus.Boarding)]
    [InlineData(TripStatus.Boarding, TripStatus.Dispatched)]
    [InlineData(TripStatus.Delayed, TripStatus.Ready)]
    [InlineData(TripStatus.Dispatched, TripStatus.Completed)]
    public async Task ValidTransition_IsAccepted(TripStatus from, TripStatus to)
    {
        var (factory, tripId) = await ArrangeAsync(from);
        using var _ = factory;
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PatchAsJsonAsync($"/api/v1/trips/{tripId}/status", new { status = to.ToString() });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Trips.FindAsync(tripId).AsTask());
        Assert.Equal(to, stored!.Status);
    }

    [Theory]
    [InlineData(TripStatus.Scheduled, TripStatus.Completed)]
    [InlineData(TripStatus.Scheduled, TripStatus.Dispatched)]
    [InlineData(TripStatus.Ready, TripStatus.Completed)]
    [InlineData(TripStatus.Completed, TripStatus.Scheduled)]
    [InlineData(TripStatus.Completed, TripStatus.Cancelled)]
    [InlineData(TripStatus.Cancelled, TripStatus.Ready)]
    [InlineData(TripStatus.Dispatched, TripStatus.Ready)]
    public async Task InvalidTransition_IsRejected_AndStatusIsUnchanged(TripStatus from, TripStatus to)
    {
        var (factory, tripId) = await ArrangeAsync(from);
        using var _ = factory;
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PatchAsJsonAsync($"/api/v1/trips/{tripId}/status", new { status = to.ToString() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Trips.FindAsync(tripId).AsTask());
        Assert.Equal(from, stored!.Status);
    }

    [Fact]
    public async Task Dispatching_RecordsActualDeparture_AndCompleting_RecordsCompletionTime()
    {
        var (factory, tripId) = await ArrangeAsync(TripStatus.Boarding);
        using var _ = factory;
        using var client = factory.CreateClientAs("Admin");

        await client.PatchAsJsonAsync($"/api/v1/trips/{tripId}/status", new { status = "Dispatched" });
        var afterDispatch = await factory.WithDbAsync(db => db.Trips.FindAsync(tripId).AsTask());
        await client.PatchAsJsonAsync($"/api/v1/trips/{tripId}/status", new { status = "Completed" });
        var afterComplete = await factory.WithDbAsync(db => db.Trips.FindAsync(tripId).AsTask());

        Assert.NotNull(afterDispatch!.ActualDepartureAt);
        Assert.Null(afterDispatch.CompletedAt);
        Assert.NotNull(afterComplete!.CompletedAt);
    }

    [Fact]
    public async Task StatusChange_OnUnknownTrip_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PatchAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}/status", new { status = "Ready" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<(TestApiFactory Factory, Guid TripId)> ArrangeAsync(TripStatus status)
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world, status: status);
        return (factory, trip.Id);
    }
}
