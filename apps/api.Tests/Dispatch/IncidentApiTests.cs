using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C: incident handling for staff, including centre-level data isolation.</summary>
public sealed class IncidentApiTests
{
    private const string Url = "/api/v1/incidents";

    private static object NewIncident(Guid centreId, Guid? tripId = null, string title = " Engine fault ", string severity = "High", string type = "Breakdown") => new
    {
        centreId, tripId, reportedByName = " Control room ", type, severity, title, description = " Bus stopped at bay 3 "
    };

    private static async Task<Guid> SeedIncidentAsync(TestApiFactory factory, Guid centreId, Guid? tripId = null, IncidentStatus status = IncidentStatus.Open,
        IncidentType type = IncidentType.Breakdown, IncidentSeverity severity = IncidentSeverity.Medium) =>
        await factory.WithDbAsync(async db =>
        {
            var incident = new Incident
            {
                CentreId = centreId, TripId = tripId, ReportedByName = "Seed", Type = type, Severity = severity, Status = status,
                Title = "Seeded", Description = "Seeded incident", SlaDueAt = DateTime.UtcNow.AddHours(2)
            };
            db.Add(incident);
            await db.SaveChangesAsync();
            return incident.Id;
        });

    [Fact]
    public async Task Create_ByManagerOfTheCentre_SavesTrimmedIncidentWithADefaultSla()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, NewIncident(world.Centre.Id, trip.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var stored = await factory.WithDbAsync(db => db.Incidents.FindAsync(id).AsTask());
        Assert.Equal("Engine fault", stored!.Title);
        Assert.Equal("Control room", stored.ReportedByName);
        Assert.Equal(IncidentStatus.Open, stored.Status);
        Assert.InRange(stored.SlaDueAt, DateTime.UtcNow.AddHours(3.9), DateTime.UtcNow.AddHours(4.1));
    }

    [Fact]
    public async Task Create_ForAnotherCentre_IsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("CentreManager", centreId: Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Url, NewIncident(world.Centre.Id))).StatusCode);
        Assert.Equal(0, await factory.CountAsync<Incident>());
    }

    [Fact]
    public async Task Create_ForUnknownCentre_OrTripOfAnotherCentre_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTH", "OTH-1", "LO");
        var otherTrip = await TestWorld.AddTripAsync(factory, other);
        using var admin = factory.CreateClientAs("Admin");

        var unknownCentre = await admin.PostAsJsonAsync(Url, NewIncident(Guid.NewGuid()));
        var wrongTrip = await admin.PostAsJsonAsync(Url, NewIncident(world.Centre.Id, otherTrip.Id));

        Assert.Equal(HttpStatusCode.BadRequest, unknownCentre.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrongTrip.StatusCode);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Title", "")]
    public async Task Create_WithMissingTitleOrDescription_IsRejected(string title, string description)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var admin = factory.CreateClientAs("Admin");

        var response = await admin.PostAsJsonAsync(Url, new { centreId = world.Centre.Id, reportedByName = "x", type = "Delay", title, description });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ForAManager_ShowsOnlyTheirCentresIncidents_AndRefusesOtherCentreFilters()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var mine = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTH", "OTH-1", "LO");
        await SeedIncidentAsync(factory, mine.Centre.Id);
        await SeedIncidentAsync(factory, other.Centre.Id);
        using var manager = factory.CreateClientAs("CentreManager", centreId: mine.Centre.Id);
        using var admin = factory.CreateClientAs("Admin");

        var managerList = await manager.GetFromJsonAsync<JsonElement>(Url);
        var adminList = await admin.GetFromJsonAsync<JsonElement>(Url);
        var peek = await manager.GetAsync($"{Url}?centreId={other.Centre.Id}");

        Assert.Equal(1, managerList.GetArrayLength());
        Assert.Equal(mine.Centre.Id, managerList[0].GetProperty("centreId").GetGuid());
        Assert.Equal(2, adminList.GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, peek.StatusCode);
    }

    [Fact]
    public async Task List_ForAStaffAccountWithoutACentre_IsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Dispatcher");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Url)).StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatusTypeSeverityAndTrip()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        await SeedIncidentAsync(factory, world.Centre.Id, trip.Id, IncidentStatus.Open, IncidentType.Breakdown, IncidentSeverity.High);
        await SeedIncidentAsync(factory, world.Centre.Id, null, IncidentStatus.Resolved, IncidentType.Delay, IncidentSeverity.Low);
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Resolved")).GetArrayLength());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Url}?type=Breakdown")).GetArrayLength());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Url}?severity=Low")).GetArrayLength());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Url}?tripId={trip.Id}")).GetArrayLength());
    }

    [Fact]
    public async Task Get_ReturnsTheIncident_ButHidesOtherCentresIncidentsAsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var mine = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTH", "OTH-1", "LO");
        var ownId = await SeedIncidentAsync(factory, mine.Centre.Id);
        var foreignId = await SeedIncidentAsync(factory, other.Centre.Id);
        using var manager = factory.CreateClientAs("FleetOfficer", centreId: mine.Centre.Id);

        var own = await manager.GetAsync($"{Url}/{ownId}");
        var foreign = await manager.GetAsync($"{Url}/{foreignId}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Update_ToResolved_SetsResolvedAt_AndReopeningClearsIt()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var id = await SeedIncidentAsync(factory, world.Centre.Id);
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);
        object Body(string status) => new { severity = "High", title = "Updated", description = "Fixed", assignedTo = " Nimal ", status, slaDueAt = DateTime.UtcNow.AddHours(1) };

        var resolved = await client.PutAsJsonAsync($"{Url}/{id}", Body("Resolved"));
        var afterResolve = await factory.WithDbAsync(db => db.Incidents.FindAsync(id).AsTask());
        var reopened = await client.PutAsJsonAsync($"{Url}/{id}", Body("InProgress"));
        var afterReopen = await factory.WithDbAsync(db => db.Incidents.FindAsync(id).AsTask());

        Assert.Equal(HttpStatusCode.NoContent, resolved.StatusCode);
        Assert.NotNull(afterResolve!.ResolvedAt);
        Assert.Equal("Nimal", afterResolve.AssignedTo);
        Assert.Equal(HttpStatusCode.NoContent, reopened.StatusCode);
        Assert.Null(afterReopen!.ResolvedAt);
    }

    [Fact]
    public async Task Update_OfAnotherCentresIncident_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var mine = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTH", "OTH-1", "LO");
        var foreignId = await SeedIncidentAsync(factory, other.Centre.Id);
        using var manager = factory.CreateClientAs("CentreManager", centreId: mine.Centre.Id);

        var response = await manager.PutAsJsonAsync($"{Url}/{foreignId}", new
        {
            severity = "Low", title = "Hijack", description = "x", status = "Resolved", slaDueAt = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Seeded", (await factory.WithDbAsync(db => db.Incidents.FindAsync(foreignId).AsTask()))!.Title);
    }

    [Theory]
    [InlineData("Commuter")]
    [InlineData("Driver")]
    public async Task Incidents_AreHiddenFromCommutersAndDrivers(string role)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Url)).StatusCode);
    }

    [Fact]
    public async Task Incidents_RequireLogin()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Url)).StatusCode);
    }
}
