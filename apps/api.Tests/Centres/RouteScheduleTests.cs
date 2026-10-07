using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Centres;

/// <summary>Component A: recurring timetables and the automatic trip generator.</summary>
public sealed class RouteScheduleTests
{
    private const string Monday = "2030-06-03";
    private const string Saturday = "2030-06-08";

    private sealed record Arranged(TestApiFactory Factory, TestWorld World, Guid ScheduleId, HttpClient Client) : IDisposable
    {
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }

    private static async Task<Arranged> ArrangeAsync(string first = "05:00:00", string last = "06:00:00", int headway = 30, string days = "Everyday")
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var client = factory.CreateClientAs("Admin");
        client.Timeout = TimeSpan.FromSeconds(15);
        var created = await client.PostAsJsonAsync($"/api/v1/routes/{world.Route.Id}/schedules", new
        {
            bayId = world.Bay.Id, firstDeparture = first, lastDeparture = last, headwayMinutes = headway, operatingDays = days
        });
        created.EnsureSuccessStatusCode();
        var scheduleId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return new Arranged(factory, world, scheduleId, client);
    }

    private static Task<HttpResponseMessage> Generate(Arranged a, string date) =>
        a.Client.PostAsJsonAsync($"/api/v1/routes/schedules/{a.ScheduleId}/generate-trips", new { serviceDate = date });

    // ---- schedule maintenance ----

    [Fact]
    public async Task ListSchedules_ReturnsTheRoutesTimetable()
    {
        using var a = await ArrangeAsync();

        var list = await a.Client.GetFromJsonAsync<JsonElement>($"/api/v1/routes/{a.World.Route.Id}/schedules");

        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal(30, list[0].GetProperty("headwayMinutes").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.GetAsync($"/api/v1/routes/{Guid.NewGuid()}/schedules")).StatusCode);
    }

    [Fact]
    public async Task UpdateSchedule_ChangesTimes_AndRejectsABadRangeOrForeignBay()
    {
        using var a = await ArrangeAsync();
        var other = await TestWorld.SeedAsync(a.Factory, "OTH", "OTH-1", "LO");
        object Body(Guid bay, string first, string last) => new { bayId = bay, firstDeparture = first, lastDeparture = last, headwayMinutes = 20, operatingDays = " Mon,Tue ", isActive = true };

        var ok = await a.Client.PutAsJsonAsync($"/api/v1/routes/schedules/{a.ScheduleId}", Body(a.World.Bay.Id, "06:00:00", "10:00:00"));
        var badRange = await a.Client.PutAsJsonAsync($"/api/v1/routes/schedules/{a.ScheduleId}", Body(a.World.Bay.Id, "10:00:00", "06:00:00"));
        var foreignBay = await a.Client.PutAsJsonAsync($"/api/v1/routes/schedules/{a.ScheduleId}", Body(other.Bay.Id, "06:00:00", "10:00:00"));
        var unknown = await a.Client.PutAsJsonAsync($"/api/v1/routes/schedules/{Guid.NewGuid()}", Body(a.World.Bay.Id, "06:00:00", "10:00:00"));

        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        var stored = await a.Factory.WithDbAsync(db => db.RouteSchedules.FindAsync(a.ScheduleId).AsTask());
        Assert.Equal(20, stored!.HeadwayMinutes);
        Assert.Equal("Mon,Tue", stored.OperatingDays);
        Assert.Equal(HttpStatusCode.BadRequest, badRange.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, foreignBay.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task DeactivateSchedule_KeepsTheRecord_AndStopsTripGeneration()
    {
        using var a = await ArrangeAsync();

        var delete = await a.Client.DeleteAsync($"/api/v1/routes/schedules/{a.ScheduleId}");
        var generate = await Generate(a, Monday);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False((await a.Factory.WithDbAsync(db => db.RouteSchedules.FindAsync(a.ScheduleId).AsTask()))!.IsActive);
        Assert.Equal(HttpStatusCode.BadRequest, generate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.DeleteAsync($"/api/v1/routes/schedules/{Guid.NewGuid()}")).StatusCode);
    }

    // ---- trip generation ----

    [Fact]
    public async Task GenerateTrips_CreatesOneTripPerDeparture_InSriLankaTime()
    {
        using var a = await ArrangeAsync("05:00:00", "06:00:00", 30);
        await TestWorld.AddVehicleAsync(a.Factory, a.World.Centre.Id, "SPARE-1");
        await TestWorld.AddDriverAsync(a.Factory, a.World.Centre.Id, "SPARE-LIC");

        var response = await Generate(a, Monday);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("created").GetInt32());
        Assert.Equal(3, body.GetProperty("planned").GetInt32());
        Assert.Equal(["05:00", "05:30", "06:00"], body.GetProperty("createdDepartures").EnumerateArray().Select(x => x.GetString()).ToArray());
        var times = await a.Factory.WithDbAsync(db => db.Trips.OrderBy(t => t.ScheduledTime).Select(t => t.ScheduledTime).ToListAsync());
        Assert.Equal(new DateTime(2030, 6, 2, 23, 30, 0, DateTimeKind.Utc), times[0]); // 05:00 in Colombo (UTC+5:30)
        Assert.All(await a.Factory.WithDbAsync(db => db.Trips.ToListAsync()), t => Assert.Equal(a.World.Bay.Id, t.BayId));
    }

    [Fact]
    public async Task GenerateTrips_RunTwice_DoesNotDuplicateTrips()
    {
        using var a = await ArrangeAsync("05:00:00", "06:00:00", 30);
        await TestWorld.AddVehicleAsync(a.Factory, a.World.Centre.Id, "SPARE-1");
        await TestWorld.AddDriverAsync(a.Factory, a.World.Centre.Id, "SPARE-LIC");

        await Generate(a, Monday);
        var again = await (await Generate(a, Monday)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, again.GetProperty("created").GetInt32());
        Assert.Equal(3, again.GetProperty("existing").GetInt32());
        Assert.Equal(3, await a.Factory.CountAsync<Trip>());
    }

    [Fact]
    public async Task GenerateTrips_WithOneBusAndOneDriver_SkipsOnlyTheDeparturesThatWouldOverlap()
    {
        using var a = await ArrangeAsync("05:00:00", "06:00:00", 30); // route takes 60 minutes, so a single bus can do only one

        var body = await (await Generate(a, Monday)).Content.ReadFromJsonAsync<JsonElement>();

        // 05:00 runs until 06:00, so 05:30 is skipped, while 06:00 starts exactly when the bus is free again.
        Assert.Equal(2, body.GetProperty("created").GetInt32());
        Assert.Equal(1, body.GetProperty("conflicts").GetInt32());
        Assert.Equal("05:30", body.GetProperty("skippedDepartures")[0].GetProperty("time").GetString());
        Assert.Equal(2, await a.Factory.CountAsync<Trip>());
    }

    [Theory]
    [InlineData("Weekdays", Saturday)]
    [InlineData("Weekends", Monday)]
    [InlineData("Mon,Wed", Saturday)]
    public async Task GenerateTrips_OnADayTheScheduleDoesNotOperate_IsRejected(string days, string date)
    {
        using var a = await ArrangeAsync(days: days);

        var response = await Generate(a, date);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await a.Factory.CountAsync<Trip>());
    }

    [Theory]
    [InlineData("Weekdays", Monday)]
    [InlineData("Weekends", Saturday)]
    [InlineData("Sat,Sun", Saturday)]
    [InlineData("mon", Monday)]
    public async Task GenerateTrips_OnAnOperatingDay_IsAccepted(string days, string date)
    {
        using var a = await ArrangeAsync("05:00:00", "05:30:00", 60, days);

        Assert.Equal(HttpStatusCode.OK, (await Generate(a, date)).StatusCode);
    }

    [Fact]
    public async Task GenerateTrips_WithoutAnActiveVehicleOrDriver_OrWithAnUnavailableBay_IsRejected()
    {
        using var noVehicle = await ArrangeAsync();
        await noVehicle.Factory.WithDbAsync(async db => { (await db.Vehicles.FirstAsync()).Status = VehicleStatus.Maintenance; await db.SaveChangesAsync(); });
        using var badBay = await ArrangeAsync();
        await badBay.Factory.WithDbAsync(async db => { (await db.Bays.FirstAsync()).Status = BayStatus.OutOfService; await db.SaveChangesAsync(); });

        Assert.Equal(HttpStatusCode.BadRequest, (await Generate(noVehicle, Monday)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Generate(badBay, Monday)).StatusCode);
    }

    [Fact]
    public async Task GenerateTrips_ForAnUnknownSchedule_ReturnsNotFound()
    {
        using var a = await ArrangeAsync();

        var response = await a.Client.PostAsJsonAsync($"/api/v1/routes/schedules/{Guid.NewGuid()}/generate-trips", new { serviceDate = Monday });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- departure times (pure function, so a regression fails fast instead of hanging a request) ----

    [Theory]
    [InlineData("05:00", "06:00", 30, "05:00,05:30,06:00")]
    [InlineData("05:00", "05:00", 30, "05:00")]
    [InlineData("05:00", "05:50", 20, "05:00,05:20,05:40")]
    [InlineData("22:30", "23:30", 30, "22:30,23:00,23:30")]   // last departure within one headway of midnight
    [InlineData("23:00", "23:59", 60, "23:00")]
    [InlineData("00:00", "01:00", 30, "00:00,00:30,01:00")]
    public void Departures_StopAtTheLastDeparture_EvenNearMidnight(string first, string last, int headway, string expected)
    {
        var times = api.Services.ScheduleTimes.Departures(TimeOnly.Parse(first), TimeOnly.Parse(last), headway).Take(500).ToList();

        Assert.Equal(expected, string.Join(",", times.Select(t => t.ToString("HH:mm"))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Departures_WithANonPositiveHeadway_ProducesNothing(int headway)
    {
        Assert.Empty(api.Services.ScheduleTimes.Departures(new TimeOnly(5, 0), new TimeOnly(6, 0), headway).Take(10));
    }

    [Fact]
    public void Departures_NeverExceedOnePerHeadwayInADay()
    {
        var times = api.Services.ScheduleTimes.Departures(new TimeOnly(0, 0), new TimeOnly(23, 59), 1).Take(5000).ToList();

        Assert.Equal(24 * 60, times.Count);
        Assert.Equal(times.Count, times.Distinct().Count());
    }

    // ---- directions ----

    [Fact]
    public async Task CreateDirection_AddsASecondDirection_AndRejectsInvalidEndpoints()
    {
        using var a = await ArrangeAsync();
        var other = await TestWorld.SeedAsync(a.Factory, "KAN", "KAN-1", "LK");
        object Body(Guid start, Guid end) => new
        {
            startCentreId = start, endCentreId = end, name = " Colombo to Kandy ", distanceKm = 115, estimatedDurationMin = 180,
            stops = new[] { new { stopName = "A", latitude = 1, longitude = 1 }, new { stopName = "B", latitude = 2, longitude = 2 } }
        };

        var ok = await a.Client.PostAsJsonAsync($"/api/v1/routes/{a.World.Route.Id}/directions", Body(a.World.Centre.Id, other.Centre.Id));
        var same = await a.Client.PostAsJsonAsync($"/api/v1/routes/{a.World.Route.Id}/directions", Body(a.World.Centre.Id, a.World.Centre.Id));
        var unknown = await a.Client.PostAsJsonAsync($"/api/v1/routes/{a.World.Route.Id}/directions", Body(a.World.Centre.Id, Guid.NewGuid()));
        var noRoute = await a.Client.PostAsJsonAsync($"/api/v1/routes/{Guid.NewGuid()}/directions", Body(a.World.Centre.Id, other.Centre.Id));

        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, noRoute.StatusCode);
        var directions = await a.Client.GetFromJsonAsync<JsonElement>($"/api/v1/routes/{a.World.Route.Id}/directions");
        Assert.Equal(1, directions.GetArrayLength());
        Assert.Equal("Colombo to Kandy", directions[0].GetProperty("name").GetString());
    }
}
