using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Tests.Shared;
using Xunit;
using RouteModel = api.Models.Route;

namespace api.Tests.Centres;

/// <summary>Component A (Centres and Network): route CRUD, ordered stops, archive and timetable rules.</summary>
public sealed class RouteCrudTests
{
    private const string Url = "/api/v1/routes";

    private static object ValidRoute(Guid centreId, string number = "01", int stopCount = 2) => new
    {
        centreId, routeNumber = number, name = "Colombo - Kandy", origin = "Colombo", destination = "Kandy",
        serviceType = "Normal", distanceKm = 115.5, estimatedDurationMin = 180,
        stops = Enumerable.Range(0, stopCount).Select(i => new { stopName = $"Stop {i}", latitude = 6.9 + i, longitude = 79.8 + i }).ToArray()
    };

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndNormalizesRouteNumber()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "  01-ac "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("01-AC", body.GetProperty("routeNumber").GetString());
    }

    [Fact]
    public async Task Create_StoresStopsInOrder()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var created = await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "05", 3));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var detail = await client.GetFromJsonAsync<JsonElement>($"{Url}/{id}");

        var stops = detail.GetProperty("stops");
        Assert.Equal(3, stops.GetArrayLength());
        Assert.Equal("Stop 0", stops[0].GetProperty("stopName").GetString());
        Assert.Equal("Stop 2", stops[2].GetProperty("stopName").GetString());
    }

    [Fact]
    public async Task Create_WithDuplicateRouteNumberAtCentre_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "07"));

        var response = await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "07"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForUnknownCentre_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidRoute(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithFewerThanTwoStops_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "09", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(10, 0)]
    [InlineData(10, 1441)]
    public async Task Create_WithOutOfRangeDistanceOrDuration_ReturnsBadRequest(double distance, int duration)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, new
        {
            centreId = world.Centre.Id, routeNumber = "X1", name = "N", origin = "A", destination = "B",
            distanceKm = distance, estimatedDurationMin = duration,
            stops = new[] { new { stopName = "A", latitude = 1, longitude = 1 }, new { stopName = "B", latitude = 2, longitude = 2 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDirectionEndpoints_CreatesADirection()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var from = await TestWorld.SeedAsync(factory, "COL", "NB-1", "L1");
        var to = await TestWorld.SeedAsync(factory, "KAN", "NB-2", "L2");
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, new
        {
            centreId = from.Centre.Id, routeNumber = "99", name = "Inter-centre", origin = "x", destination = "y",
            distanceKm = 100, estimatedDurationMin = 150, startCentreId = from.Centre.Id, endCentreId = to.Centre.Id,
            stops = new[] { new { stopName = "A", latitude = 1, longitude = 1 }, new { stopName = "B", latitude = 2, longitude = 2 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var directions = await client.GetFromJsonAsync<JsonElement>($"{Url}/{id}/directions");
        Assert.Equal(1, directions.GetArrayLength());
    }

    [Fact]
    public async Task Create_WithSameStartAndEndCentre_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, new
        {
            centreId = world.Centre.Id, routeNumber = "98", name = "Loop", origin = "x", destination = "y",
            distanceKm = 10, estimatedDurationMin = 30, startCentreId = world.Centre.Id, endCentreId = world.Centre.Id,
            stops = new[] { new { stopName = "A", latitude = 1, longitude = 1 }, new { stopName = "B", latitude = 2, longitude = 2 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesRoute_AndReplacesStops()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        var created = await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "21", 3));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new
        {
            name = "Renamed", origin = "P", destination = "Q", serviceType = "AcExpress", distanceKm = 20, estimatedDurationMin = 45,
            isActive = true,
            stops = new[] { new { stopName = "P", latitude = 1, longitude = 1 }, new { stopName = "Q", latitude = 2, longitude = 2 } }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var detail = await client.GetFromJsonAsync<JsonElement>($"{Url}/{id}");
        Assert.Equal("Renamed", detail.GetProperty("name").GetString());
        Assert.Equal(2, detail.GetProperty("stops").GetArrayLength());
    }

    [Fact]
    public async Task UpdateDirection_ReplacesStops_AndKeepsThemOrdered()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var from = await TestWorld.SeedAsync(factory, "COL", "NB-1", "L1");
        var to = await TestWorld.SeedAsync(factory, "KAN", "NB-2", "L2");
        using var client = factory.CreateClientAs("Admin");
        var created = await client.PostAsJsonAsync(Url, new
        {
            centreId = from.Centre.Id, routeNumber = "77", name = "Inter-centre", origin = "x", destination = "y",
            distanceKm = 100, estimatedDurationMin = 150, startCentreId = from.Centre.Id, endCentreId = to.Centre.Id,
            stops = new[] { new { stopName = "A", latitude = 1, longitude = 1 }, new { stopName = "B", latitude = 2, longitude = 2 } }
        });
        var routeId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var directions = await client.GetFromJsonAsync<JsonElement>($"{Url}/{routeId}/directions");
        var directionId = directions[0].GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"{Url}/directions/{directionId}", new
        {
            startCentreId = from.Centre.Id, endCentreId = to.Centre.Id, name = "Renamed direction", distanceKm = 90,
            estimatedDurationMin = 120, isActive = true,
            stops = new[]
            {
                new { stopName = "X", latitude = 1, longitude = 1 }, new { stopName = "Y", latitude = 2, longitude = 2 },
                new { stopName = "Z", latitude = 3, longitude = 3 }
            }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await client.GetFromJsonAsync<JsonElement>($"{Url}/{routeId}/directions");
        var stops = updated[0].GetProperty("stops");
        Assert.Equal(3, stops.GetArrayLength());
        Assert.Equal("X", stops[0].GetProperty("stopName").GetString());
        Assert.Equal("Z", stops[2].GetProperty("stopName").GetString());
    }

    [Fact]
    public async Task Archive_DeactivatesRoute_ButKeepsRecord_AndReactivateRestoresIt()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var archived = await client.DeleteAsync($"{Url}/{world.Route.Id}");
        var afterArchive = await factory.WithDbAsync(db => db.Routes.FindAsync(world.Route.Id).AsTask());
        var reactivated = await client.PostAsync($"{Url}/{world.Route.Id}/reactivate", null);
        var afterReactivate = await factory.WithDbAsync(db => db.Routes.FindAsync(world.Route.Id).AsTask());

        Assert.Equal(HttpStatusCode.NoContent, archived.StatusCode);
        Assert.False(afterArchive!.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, reactivated.StatusCode);
        Assert.True(afterReactivate!.IsActive);
    }

    [Fact]
    public async Task List_FiltersByActiveFlag_AndSearchTerm()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "11"));
        var archivedId = (await (await client.PostAsJsonAsync(Url, ValidRoute(world.Centre.Id, "12"))).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.DeleteAsync($"{Url}/{archivedId}");

        var active = await client.GetFromJsonAsync<JsonElement>($"{Url}?active=true&search=colombo");
        var inactive = await client.GetFromJsonAsync<JsonElement>($"{Url}?active=false");

        Assert.Equal(1, active.GetArrayLength()); // only route 11 mentions Colombo and is active
        Assert.Equal(1, inactive.GetArrayLength());
    }

    [Fact]
    public async Task Get_UnknownRoute_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.GetAsync($"{Url}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Timetables ----

    [Fact]
    public async Task CreateSchedule_WithValidData_ReturnsCreated()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{world.Route.Id}/schedules", new
        {
            bayId = world.Bay.Id, firstDeparture = "05:00:00", lastDeparture = "20:00:00", headwayMinutes = 30, operatingDays = "Mon,Tue,Wed"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_WhenLastDepartureIsNotAfterFirst_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{world.Route.Id}/schedules", new
        {
            bayId = world.Bay.Id, firstDeparture = "20:00:00", lastDeparture = "05:00:00", headwayMinutes = 30, operatingDays = "Mon"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_WithBayFromAnotherCentre_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTH", "NB-9", "L9");
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{world.Route.Id}/schedules", new
        {
            bayId = other.Bay.Id, firstDeparture = "05:00:00", lastDeparture = "20:00:00", headwayMinutes = 30, operatingDays = "Mon"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSchedule_WithZeroHeadway_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{world.Route.Id}/schedules", new
        {
            bayId = world.Bay.Id, firstDeparture = "05:00:00", lastDeparture = "20:00:00", headwayMinutes = 0, operatingDays = "Mon"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
