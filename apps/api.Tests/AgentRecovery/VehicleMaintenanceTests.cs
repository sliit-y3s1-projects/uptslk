using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Data;
using api.Enums;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class VehicleMaintenanceTests
{
    [Fact]
    public void ScheduledMaintenance_BlocksSriLankanServiceDayAndOvernightTrips()
    {
        var record = new MaintenanceRecord
        {
            Status = MaintenanceStatus.Scheduled,
            ScheduledFor = new DateTime(2026, 10, 1, 3, 30, 0, DateTimeKind.Utc)
        };

        Assert.True(VehicleMaintenanceRules.BlocksTrip(record, new DateTime(2026, 9, 30, 18, 0, 0, DateTimeKind.Utc), 120));
        Assert.True(VehicleMaintenanceRules.BlocksTrip(record, new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc), 60));
        Assert.False(VehicleMaintenanceRules.BlocksTrip(record, new DateTime(2026, 10, 1, 19, 0, 0, DateTimeKind.Utc), 60));

        record.Status = MaintenanceStatus.InProgress;
        Assert.True(VehicleMaintenanceRules.BlocksTrip(record, new DateTime(2026, 10, 2, 4, 0, 0, DateTimeKind.Utc), 60));
        record.Status = MaintenanceStatus.Completed;
        Assert.False(VehicleMaintenanceRules.BlocksTrip(record, new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc), 60));
    }

    [Fact]
    public async Task ScheduledMaintenance_RejectsExistingTrips_ThenBlocksNewAssignments()
    {
        using var factory = new RecoveryApiFactory();
        await factory.InitializeDatabaseAsync();
        var (centreId, vehicleId, driverId, bayId, tripId) = await SeedTripAsync(factory);
        using var client = CreateClient(factory, centreId);
        var request = new
        {
            vehicleId, type = "Brake service", description = "Inspect brakes",
            scheduledFor = new DateTime(2026, 10, 1, 3, 30, 0, DateTimeKind.Utc)
        };

        var conflict = await client.PostAsJsonAsync("/api/v1/maintenance-records", request);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trip = await db.Trips.FindAsync(tripId);
            trip!.Status = TripStatus.Cancelled;
            await db.SaveChangesAsync();
        }

        var created = await client.PostAsJsonAsync("/api/v1/maintenance-records", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Guid scheduleId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var conflicts = await new TripConflictService(db).FindConflicts(
                vehicleId, driverId, bayId,
                new DateTime(2026, 10, 1, 6, 0, 0, DateTimeKind.Utc), 60);
            Assert.Contains(conflicts, item => item.Contains("maintenance", StringComparison.OrdinalIgnoreCase));
            var trip = await db.Trips.FindAsync(tripId);
            var schedule = new RouteSchedule
            {
                RouteId = trip!.RouteId, BayId = bayId,
                FirstDeparture = new TimeOnly(11, 30),
                LastDeparture = new TimeOnly(11, 30), HeadwayMinutes = 30
            };
            db.Add(schedule);
            await db.SaveChangesAsync();
            scheduleId = schedule.Id;
        }

        var generation = await client.PostAsJsonAsync(
            $"/api/v1/routes/schedules/{scheduleId}/generate-trips",
            new { serviceDate = "2026-10-01" });
        Assert.Equal(HttpStatusCode.OK, generation.StatusCode);
        using var result = JsonDocument.Parse(await generation.Content.ReadAsStringAsync());
        Assert.Equal(0, result.RootElement.GetProperty("created").GetInt32());
        Assert.Equal(1, result.RootElement.GetProperty("conflicts").GetInt32());
    }

    [Fact]
    public async Task MaintenanceMutation_RequiresAnAuthorisedCentreMember()
    {
        using var factory = new RecoveryApiFactory();
        await factory.InitializeDatabaseAsync();
        var (_, vehicleId, _, _, _) = await SeedTripAsync(factory);
        var request = new
        {
            vehicleId, type = "Service", description = "Inspect bus",
            scheduledFor = new DateTime(2026, 10, 2, 3, 30, 0, DateTimeKind.Utc)
        };
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/v1/maintenance-records", request)).StatusCode);

        using var otherCentre = CreateClient(factory, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherCentre.PostAsJsonAsync("/api/v1/maintenance-records", request)).StatusCode);
    }

    private static HttpClient CreateClient(RecoveryApiFactory factory, Guid centreId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "CentreManager");
        client.DefaultRequestHeaders.Add("X-Test-Centre-Id", centreId.ToString());
        return client;
    }

    private static async Task<(Guid CentreId, Guid VehicleId, Guid DriverId, Guid BayId, Guid TripId)> SeedTripAsync(RecoveryApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var centre = new Centre { Code = "KAD", Name = "Kadawatha", City = "Kadawatha", District = "Gampaha" };
        db.Add(centre);
        await db.SaveChangesAsync();
        var route = new Route { CentreId = centre.Id, RouteNumber = "EX-1", Name = "Express", Origin = "Kadawatha", Destination = "Colombo", EstimatedDurationMin = 60 };
        var vehicle = new Vehicle { CentreId = centre.Id, PlateNumber = "MAINT-100", Model = "Bus", Capacity = 45 };
        var driver = new Driver { CentreId = centre.Id, FullName = "Test Driver", LicenseNumber = "MAINT-LIC" };
        var bay = new Bay { CentreId = centre.Id, Code = "B01" };
        db.AddRange(route, vehicle, driver, bay);
        await db.SaveChangesAsync();
        var trip = new Trip
        {
            CentreId = centre.Id, RouteId = route.Id, VehicleId = vehicle.Id,
            DriverId = driver.Id, BayId = bay.Id,
            ScheduledTime = new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc)
        };
        db.Add(trip);
        await db.SaveChangesAsync();
        return (centre.Id, vehicle.Id, driver.Id, bay.Id, trip.Id);
    }
}
