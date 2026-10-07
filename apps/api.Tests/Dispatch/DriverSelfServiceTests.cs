using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C: what a driver can do from the mobile app (profile, duties, status, incident reports).</summary>
public sealed class DriverSelfServiceTests
{
    private sealed record Arranged(TestApiFactory Factory, TestWorld World, Guid UserId, Trip Trip, HttpClient Client) : IDisposable
    {
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }

    private static async Task<Arranged> ArrangeAsync(TripStatus status = TripStatus.Scheduled)
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var userId = await factory.WithDbAsync(async db =>
        {
            var user = new User { UserName = "drv@test.lk", Email = "drv@test.lk", Name = "Driver", Role = UserRole.Driver, CentreId = world.Centre.Id };
            db.Add(user);
            await db.SaveChangesAsync();
            (await db.Drivers.FindAsync(world.Driver.Id))!.UserId = user.Id;
            await db.SaveChangesAsync();
            return user.Id;
        });
        var trip = await TestWorld.AddTripAsync(factory, world, status: status);
        return new Arranged(factory, world, userId, trip, factory.CreateClientAs("Driver", userId, world.Centre.Id));
    }

    [Fact]
    public async Task Me_ReturnsTheLinkedDriverProfile()
    {
        using var a = await ArrangeAsync();

        var body = await a.Client.GetFromJsonAsync<JsonElement>("/api/v1/drivers/me");

        Assert.Equal(a.World.Driver.Id, body.GetProperty("id").GetGuid());
        Assert.Equal(a.World.Centre.Code, body.GetProperty("centreCode").GetString());
        Assert.Equal("drv@test.lk", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_ForAnAccountWithoutADriverRecord_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Driver", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/drivers/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/drivers/me/trips")).StatusCode);
    }

    [Fact]
    public async Task MyTrips_ForASingleDate_ReturnsThatDaysDutiesWithPassengerCounts()
    {
        using var a = await ArrangeAsync();
        var day = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(a.Trip.ScheduledTime, "Asia/Colombo");

        var duties = await a.Client.GetFromJsonAsync<JsonElement>($"/api/v1/drivers/me/trips?date={day:yyyy-MM-dd}");

        Assert.Equal(1, duties.GetArrayLength());
        Assert.Equal(a.Trip.Id, duties[0].GetProperty("id").GetGuid());
        Assert.Equal(0, duties[0].GetProperty("passengerCount").GetInt32());
        Assert.Equal(40, duties[0].GetProperty("capacity").GetInt32());
    }

    [Theory]
    [InlineData("fromDate=2026-10-01")]
    [InlineData("toDate=2026-10-01")]
    [InlineData("fromDate=2026-10-05&toDate=2026-10-01")]
    [InlineData("fromDate=2026-10-01&toDate=2026-10-20")]
    public async Task MyTrips_WithAnInvalidDateRange_IsRejected(string query)
    {
        using var a = await ArrangeAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await a.Client.GetAsync($"/api/v1/drivers/me/trips?{query}")).StatusCode);
    }

    [Theory]
    [InlineData(TripStatus.Scheduled, "Ready")]
    [InlineData(TripStatus.Ready, "Boarding")]
    [InlineData(TripStatus.Boarding, "Dispatched")]
    [InlineData(TripStatus.Dispatched, "Completed")]
    public async Task DriverCanMoveATripAlongTheNormalLifecycle(TripStatus from, string to)
    {
        using var a = await ArrangeAsync(from);

        var response = await a.Client.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/status", new { status = to });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await a.Factory.WithDbAsync(db => db.Trips.FindAsync(a.Trip.Id).AsTask());
        Assert.Equal(Enum.Parse<TripStatus>(to), stored!.Status);
        if (to == "Dispatched") Assert.NotNull(stored.ActualDepartureAt);
        if (to == "Completed") Assert.NotNull(stored.CompletedAt);
    }

    [Theory]
    [InlineData(TripStatus.Scheduled, "Completed")]
    [InlineData(TripStatus.Scheduled, "Cancelled")]
    [InlineData(TripStatus.Completed, "Ready")]
    [InlineData(TripStatus.Cancelled, "Ready")]
    public async Task DriverCannotMakeAnInvalidOrCancellingTransition(TripStatus from, string to)
    {
        using var a = await ArrangeAsync(from);

        var response = await a.Client.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/status", new { status = to });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(from, (await a.Factory.WithDbAsync(db => db.Trips.FindAsync(a.Trip.Id).AsTask()))!.Status);
    }

    [Fact]
    public async Task DriverCannotChangeAnotherDriversTrip()
    {
        using var a = await ArrangeAsync();
        var otherDriver = await TestWorld.AddDriverAsync(a.Factory, a.World.Centre.Id, "OTHER-LIC");
        var otherVehicle = await TestWorld.AddVehicleAsync(a.Factory, a.World.Centre.Id, "OTHER-V");
        var otherBay = await TestWorld.AddBayAsync(a.Factory, a.World.Centre.Id, "OB");
        var otherTrip = await a.Factory.WithDbAsync(async db =>
        {
            var trip = new Trip { CentreId = a.World.Centre.Id, RouteId = a.World.Route.Id, VehicleId = otherVehicle.Id, DriverId = otherDriver.Id, BayId = otherBay.Id, ScheduledTime = TestWorld.FutureDeparture.AddHours(5) };
            db.Add(trip);
            await db.SaveChangesAsync();
            return trip;
        });

        var response = await a.Client.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{otherTrip.Id}/status", new { status = "Ready" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReportIncident_ForAFinishedTrip_IsRejected()
    {
        using var a = await ArrangeAsync(TripStatus.Completed);

        var response = await a.Client.PostAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/incidents",
            new { type = "Delay", severity = "Low", title = "Late", description = "Traffic" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await a.Factory.CountAsync<Incident>());
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Title", "  ")]
    public async Task ReportIncident_WithBlankTitleOrDescription_IsRejected(string title, string description)
    {
        using var a = await ArrangeAsync();

        var response = await a.Client.PostAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/incidents",
            new { type = "Delay", severity = "Low", title, description });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReportIncident_WithAnUnknownTypeValue_IsRejected()
    {
        using var a = await ArrangeAsync();

        var response = await a.Client.PostAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/incidents",
            new { type = 99, severity = "Low", title = "T", description = "D" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
