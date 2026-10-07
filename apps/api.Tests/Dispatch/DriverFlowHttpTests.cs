using System.Net;
using System.Net.Http.Json;
using api.Data;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Dispatch;

public sealed class DriverFlowHttpTests
{
    [Fact]
    public async Task Driver_CanSeeSevenDayDuties_AndNotOtherDriversDuties()
    {
        using var factory = await CreateFactoryAsync();
        var (userId, centreId, tripId) = await SeedDriverTripAsync(factory);
        Guid otherTripId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherUser = new User { UserName = "other@test.local", Email = "other@test.local", Name = "Other Driver", Role = UserRole.Driver, CentreId = centreId };
            db.Add(otherUser);
            await db.SaveChangesAsync();
            var otherDriver = new Driver { CentreId = centreId, UserId = otherUser.Id, FullName = otherUser.Name, LicenseNumber = "OTHER-LIC" };
            db.Add(otherDriver);
            await db.SaveChangesAsync();
            var assignedTrip = await db.Trips.AsNoTracking().SingleAsync();
            var otherTrip = new Trip
            {
                CentreId = centreId, RouteId = assignedTrip.RouteId, VehicleId = assignedTrip.VehicleId,
                BayId = assignedTrip.BayId, DriverId = otherDriver.Id,
                ScheduledTime = assignedTrip.ScheduledTime.AddHours(2)
            };
            db.Add(otherTrip);
            await db.SaveChangesAsync();
            otherTripId = otherTrip.Id;
        }
        using var client = CreateDriverClient(factory, userId, centreId);

        var response = await client.GetAsync("/api/v1/drivers/me/trips?fromDate=2026-09-30&toDate=2026-10-06");
        var duties = await response.Content.ReadFromJsonAsync<List<DriverDutyResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(duties!);
        Assert.Equal(tripId, duties![0].Id);
        var forbiddenIncident = await client.PostAsJsonAsync($"/api/v1/drivers/me/trips/{otherTripId}/incidents", new
        {
            type = "Delay", severity = "Low", title = "Not my trip", description = "Should be rejected"
        });
        Assert.Equal(HttpStatusCode.NotFound, forbiddenIncident.StatusCode);

        var invalidRange = await client.GetAsync("/api/v1/drivers/me/trips?fromDate=2026-09-30&toDate=2026-10-07");
        Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
    }

    [Fact]
    public async Task Driver_ReportsIncident_WithoutChangingTripStatus()
    {
        using var factory = await CreateFactoryAsync();
        var (userId, centreId, tripId) = await SeedDriverTripAsync(factory);
        using var client = CreateDriverClient(factory, userId, centreId);

        var response = await client.PostAsJsonAsync($"/api/v1/drivers/me/trips/{tripId}/incidents", new
        {
            type = "Breakdown", severity = "High", title = "Engine stopped", description = "Bus stopped safely at the next bay."
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var incident = await db.Incidents.SingleAsync();
        Assert.Equal(tripId, incident.TripId);
        Assert.Equal(centreId, incident.CentreId);
        Assert.Equal(userId, incident.ReportedById);
        Assert.Equal(TripStatus.Scheduled, (await db.Trips.SingleAsync()).Status);
    }

    [Fact]
    public async Task Delay_RequiresReason_AndInactiveDriverCannotUseSavedToken()
    {
        using var factory = await CreateFactoryAsync();
        var (userId, centreId, tripId) = await SeedDriverTripAsync(factory);
        using var client = CreateDriverClient(factory, userId, centreId);

        var missingReason = await client.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{tripId}/status", new { status = "Delayed" });
        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);
        var delayed = await client.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{tripId}/status", new { status = "Delayed", note = "Road blocked" });
        Assert.Equal(HttpStatusCode.NoContent, delayed.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var driver = await db.Drivers.SingleAsync();
            driver.Status = DriverStatus.Inactive;
            await db.SaveChangesAsync();
        }
        var blocked = await client.GetAsync("/api/v1/drivers/me/trips");
        Assert.Equal(HttpStatusCode.NotFound, blocked.StatusCode);
    }

    [Fact]
    public async Task GeneralIncidentApi_DoesNotAcceptDriverRole()
    {
        using var factory = await CreateFactoryAsync();
        var (userId, centreId, _) = await SeedDriverTripAsync(factory);
        using var client = CreateDriverClient(factory, userId, centreId);

        var response = await client.GetAsync("/api/v1/incidents");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record DriverDutyResponse(Guid Id);

    private static async Task<TestApiFactory> CreateFactoryAsync()
    {
        var factory = new TestApiFactory();
        await factory.InitializeDatabaseAsync();
        return factory;
    }

    private static HttpClient CreateDriverClient(TestApiFactory factory, Guid userId, Guid centreId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Driver");
        client.DefaultRequestHeaders.Add("X-Test-Centre-Id", centreId.ToString());
        return client;
    }

    private static async Task<(Guid UserId, Guid CentreId, Guid TripId)> SeedDriverTripAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var centre = new Centre { Code = "TEST", Name = "Test Centre", City = "Colombo", District = "Colombo" };
        var user = new User { UserName = "driver@test.local", Email = "driver@test.local", Name = "Test Driver", Role = UserRole.Driver, CentreId = centre.Id, IsActive = true };
        var route = new Route { CentreId = centre.Id, RouteNumber = "R1", Name = "Test Route", Origin = "A", Destination = "B" };
        var vehicle = new Vehicle { CentreId = centre.Id, PlateNumber = "TEST-1000", Model = "Bus", Capacity = 40 };
        var bay = new Bay { CentreId = centre.Id, Code = "B1" };
        db.Add(centre);
        await db.SaveChangesAsync();
        db.AddRange(user, route, vehicle, bay);
        await db.SaveChangesAsync();
        var driver = new Driver { CentreId = centre.Id, UserId = user.Id, FullName = user.Name, LicenseNumber = "TEST-LIC" };
        db.Add(driver);
        await db.SaveChangesAsync();
        var trip = new Trip
        {
            CentreId = centre.Id, RouteId = route.Id, VehicleId = vehicle.Id,
            DriverId = driver.Id, BayId = bay.Id,
            ScheduledTime = new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc)
        };
        db.Add(trip);
        await db.SaveChangesAsync();
        return (user.Id, centre.Id, trip.Id);
    }
}
