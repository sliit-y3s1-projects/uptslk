using System.Net;
using System.Net.Http.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Auth;

/// <summary>
/// Shared foundation: staff may only change data of their OWN centre. A manager of centre A must not be able to edit
/// centre B's centre, bays, routes, vehicles, trips or fares (only an Admin can work across centres).
/// </summary>
public sealed class CentreIsolationTests
{
    private sealed record Arranged(TestApiFactory Factory, TestWorld Own, TestWorld Other) : IDisposable
    {
        public HttpClient Manager(string role = "CentreManager") => Factory.CreateClientAs(role, centreId: Own.Centre.Id);
        public void Dispose() => Factory.Dispose();
    }

    private static async Task<Arranged> ArrangeAsync()
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var own = await TestWorld.SeedAsync(factory, "OWN", "OWN-1", "L-OWN");
        var other = await TestWorld.SeedAsync(factory, "OTH", "OTH-1", "L-OTH");
        return new Arranged(factory, own, other);
    }

    private static object CentreBody(string name = "Hijacked") => new { name, city = "X", district = "Y", status = "Operating" };

    // ---- centres and bays (component A) ----

    [Fact]
    public async Task Manager_CannotUpdateOrCloseAnotherCentre()
    {
        using var a = await ArrangeAsync();
        using var manager = a.Manager();

        var update = await manager.PutAsJsonAsync($"/api/v1/centres/{a.Other.Centre.Id}", CentreBody());
        var close = await manager.DeleteAsync($"/api/v1/centres/{a.Other.Centre.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, close.StatusCode);
        var stored = await a.Factory.WithDbAsync(db => db.Centres.FindAsync(a.Other.Centre.Id).AsTask());
        Assert.NotEqual("Hijacked", stored!.Name);
        Assert.Equal(CentreStatus.Operating, stored.Status);
    }

    [Fact]
    public async Task Manager_CanUpdateTheirOwnCentre_AndAdminCanUpdateAny()
    {
        using var a = await ArrangeAsync();
        using var manager = a.Manager();
        using var admin = a.Factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.NoContent, (await manager.PutAsJsonAsync($"/api/v1/centres/{a.Own.Centre.Id}", CentreBody("Mine"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/v1/centres/{a.Other.Centre.Id}", CentreBody("By admin"))).StatusCode);
    }

    [Fact]
    public async Task Manager_CannotChangeBaysOfAnotherCentre()
    {
        using var a = await ArrangeAsync();
        using var manager = a.Manager();

        var create = await manager.PostAsJsonAsync($"/api/v1/centres/{a.Other.Centre.Id}/bays", new { code = "NEW" });
        var update = await manager.PutAsJsonAsync($"/api/v1/centres/bays/{a.Other.Bay.Id}", new { code = "B-NEW", status = "Available" });
        var deactivate = await manager.DeleteAsync($"/api/v1/centres/bays/{a.Other.Bay.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
        Assert.Equal(BayStatus.Available, (await a.Factory.WithDbAsync(db => db.Bays.FindAsync(a.Other.Bay.Id).AsTask()))!.Status);
    }

    [Fact]
    public async Task Manager_CannotChangeRoutesOrTimetablesOfAnotherCentre()
    {
        using var a = await ArrangeAsync();
        using var manager = a.Manager();
        var stops = new[] { new { stopName = "A", latitude = 1, longitude = 1 }, new { stopName = "B", latitude = 2, longitude = 2 } };

        var create = await manager.PostAsJsonAsync("/api/v1/routes", new
        {
            centreId = a.Other.Centre.Id, routeNumber = "HX", name = "N", origin = "A", destination = "B", distanceKm = 5, estimatedDurationMin = 10, stops
        });
        var update = await manager.PutAsJsonAsync($"/api/v1/routes/{a.Other.Route.Id}", new
        {
            name = "Hijacked", origin = "A", destination = "B", distanceKm = 5, estimatedDurationMin = 10, isActive = true, stops
        });
        var archive = await manager.DeleteAsync($"/api/v1/routes/{a.Other.Route.Id}");
        var schedule = await manager.PostAsJsonAsync($"/api/v1/routes/{a.Other.Route.Id}/schedules", new
        {
            bayId = a.Other.Bay.Id, firstDeparture = "05:00:00", lastDeparture = "06:00:00", headwayMinutes = 30, operatingDays = "Everyday"
        });

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, archive.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, schedule.StatusCode);
        Assert.True((await a.Factory.WithDbAsync(db => db.Routes.FindAsync(a.Other.Route.Id).AsTask()))!.IsActive);
    }

    // ---- vehicles (component B) ----

    [Fact]
    public async Task Staff_CannotChangeVehiclesOfAnotherCentre()
    {
        using var a = await ArrangeAsync();
        using var officer = a.Manager("FleetOfficer");

        var create = await officer.PostAsJsonAsync("/api/v1/vehicles", new { centreId = a.Other.Centre.Id, plateNumber = "HX-1", model = "Bus", capacity = 40 });
        var update = await officer.PutAsJsonAsync($"/api/v1/vehicles/{a.Other.Vehicle.Id}", new
        {
            centreId = a.Other.Centre.Id, plateNumber = "HX-2", model = "Bus", capacity = 40
        });
        var move = await officer.PutAsJsonAsync($"/api/v1/vehicles/{a.Own.Vehicle.Id}", new
        {
            centreId = a.Other.Centre.Id, plateNumber = a.Own.Vehicle.PlateNumber, model = "Bus", capacity = 40 // stealing/dumping a bus into another centre
        });
        var deactivate = await officer.DeleteAsync($"/api/v1/vehicles/{a.Other.Vehicle.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, move.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
        Assert.Equal(VehicleStatus.Active, (await a.Factory.WithDbAsync(db => db.Vehicles.FindAsync(a.Other.Vehicle.Id).AsTask()))!.Status);
        Assert.Equal(a.Own.Centre.Id, (await a.Factory.WithDbAsync(db => db.Vehicles.FindAsync(a.Own.Vehicle.Id).AsTask()))!.CentreId);
    }

    [Fact]
    public async Task FleetOfficer_CanStillManageVehiclesOfTheirOwnCentre()
    {
        using var a = await ArrangeAsync();
        using var officer = a.Manager("FleetOfficer");

        var create = await officer.PostAsJsonAsync("/api/v1/vehicles", new { centreId = a.Own.Centre.Id, plateNumber = "OWN-2", model = "Bus", capacity = 40 });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    // ---- trips (component C) ----

    [Fact]
    public async Task Dispatcher_CannotCreateChangeOrCancelTripsOfAnotherCentre()
    {
        using var a = await ArrangeAsync();
        var foreignTrip = await TestWorld.AddTripAsync(a.Factory, a.Other);
        using var dispatcher = a.Manager("Dispatcher");

        var create = await dispatcher.PostAsJsonAsync("/api/v1/trips", TripCrudTestsBody(a.Other));
        var status = await dispatcher.PatchAsJsonAsync($"/api/v1/trips/{foreignTrip.Id}/status", new { status = "Ready" });
        var cancel = await dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{foreignTrip.Id}")
        {
            Content = JsonContent.Create(new { reason = "sabotage" })
        });

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal(TripStatus.Scheduled, (await a.Factory.WithDbAsync(db => db.Trips.FindAsync(foreignTrip.Id).AsTask()))!.Status);
    }

    [Fact]
    public async Task Dispatcher_CanStillManageTripsOfTheirOwnCentre()
    {
        using var a = await ArrangeAsync();
        var ownTrip = await TestWorld.AddTripAsync(a.Factory, a.Own);
        using var dispatcher = a.Manager("Dispatcher");

        var status = await dispatcher.PatchAsJsonAsync($"/api/v1/trips/{ownTrip.Id}/status", new { status = "Ready" });
        var create = await dispatcher.PostAsJsonAsync("/api/v1/trips", TripCrudTestsBody(a.Own, TestWorld.FutureDeparture.AddHours(6)));

        Assert.Equal(HttpStatusCode.NoContent, status.StatusCode);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    // ---- fares (component D) ----

    [Fact]
    public async Task Manager_CannotChangeFaresOfAnotherCentresRoute()
    {
        using var a = await ArrangeAsync();
        await TestWorld.AddPassengerAsync(a.Factory, a.Other, 0, 100m); // creates the fare rule of the other centre's route
        var fareId = await a.Factory.WithDbAsync(db => db.FareRules.Where(r => r.RouteId == a.Other.Route.Id).Select(r => r.Id).SingleAsync());
        using var manager = a.Manager();

        var create = await manager.PostAsJsonAsync("/api/v1/fare-rules", new { routeId = a.Own.Route.Id, amount = 100m }); // own route: allowed
        var update = await manager.PutAsJsonAsync($"/api/v1/fare-rules/{fareId}", new { amount = 0.01m, isActive = true });
        var delete = await manager.DeleteAsync($"/api/v1/fare-rules/{fareId}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(100m, (await a.Factory.WithDbAsync(db => db.FareRules.FindAsync(fareId).AsTask()))!.Amount);
    }

    [Fact]
    public async Task StaffWithoutACentreAssignment_CannotChangeCentreData()
    {
        using var a = await ArrangeAsync();
        using var unassigned = a.Factory.CreateClientAs("CentreManager"); // no centre_id claim

        var response = await unassigned.PutAsJsonAsync($"/api/v1/centres/{a.Own.Centre.Id}", CentreBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static object TripCrudTestsBody(TestWorld w, DateTime? at = null) => new
    {
        centreId = w.Centre.Id, routeId = w.Route.Id, vehicleId = w.Vehicle.Id, driverId = w.Driver.Id, bayId = w.Bay.Id,
        scheduledTime = at ?? TestWorld.FutureDeparture
    };
}
