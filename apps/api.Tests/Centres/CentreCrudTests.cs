using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Centres;

/// <summary>Component A (Centres and Network): centre and bay CRUD, validation, filters and soft-delete.</summary>
public sealed class CentreCrudTests
{
    private const string Url = "/api/v1/centres";

    private static object ValidCentre(string code = "kur", string name = "Kurunegala Centre") => new
    {
        code, name, city = "Kurunegala", district = "Kurunegala", description = "Main hub", status = "Operating"
    };

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndNormalizesCode()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidCentre("  kur "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("KUR", body.GetProperty("code").GetString());
        Assert.Equal(1, await factory.CountAsync<Centre>());
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");
        await client.PostAsJsonAsync(Url, ValidCentre("KUR"));

        var response = await client.PostAsJsonAsync(Url, ValidCentre("kur", "Another Name"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await factory.CountAsync<Centre>());
    }

    [Theory]
    [InlineData("", "Name", "City", "District")]
    [InlineData("KUR", "", "City", "District")]
    [InlineData("KUR", "Name", "", "District")]
    [InlineData("KUR", "Name", "City", "")]
    public async Task Create_WithMissingRequiredField_ReturnsBadRequest(string code, string name, string city, string district)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, new { code, name, city, district });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountAsync<Centre>());
    }

    [Fact]
    public async Task Create_WithTooLongName_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, ValidCentre("KUR", new string('x', 161)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsCentreWithBaysAndRoutes()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.GetAsync($"{Url}/{world.Centre.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(world.Centre.Code, body.GetProperty("code").GetString());
        Assert.Equal(1, body.GetProperty("bays").GetArrayLength());
        Assert.Equal(1, body.GetProperty("routes").GetArrayLength());
    }

    [Fact]
    public async Task Get_UnknownCentre_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.GetAsync($"{Url}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByDistrict_AndSearch_AndHidesClosedByDefault()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");
        await client.PostAsJsonAsync(Url, new { code = "COL", name = "Colombo Fort", city = "Colombo", district = "Colombo", status = "Operating" });
        await client.PostAsJsonAsync(Url, new { code = "KAN", name = "Kandy Hub", city = "Kandy", district = "Kandy", status = "Operating" });
        await client.PostAsJsonAsync(Url, new { code = "OLD", name = "Old Depot", city = "Galle", district = "Galle", status = "Closed" });

        var all = await client.GetFromJsonAsync<JsonElement>(Url);
        var byDistrict = await client.GetFromJsonAsync<JsonElement>($"{Url}?district=Kandy");
        var bySearch = await client.GetFromJsonAsync<JsonElement>($"{Url}?search=fort");
        var closed = await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Closed");

        Assert.Equal(2, all.GetArrayLength());
        Assert.Equal("KAN", byDistrict[0].GetProperty("code").GetString());
        Assert.Equal("COL", bySearch[0].GetProperty("code").GetString());
        Assert.Equal("OLD", closed[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_ChangesDetails_AndReturnsNoContent()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/{world.Centre.Id}", new
        {
            name = "Renamed Centre", city = "Negombo", district = "Gampaha", status = "Suspended"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Centres.FindAsync(world.Centre.Id).AsTask());
        Assert.Equal("Renamed Centre", stored!.Name);
        Assert.Equal(CentreStatus.Suspended, stored.Status);
    }

    [Fact]
    public async Task Update_UnknownCentre_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", new { name = "X", city = "Y", district = "Z" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ClosesCentre_ButKeepsTheRecord()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.DeleteAsync($"{Url}/{world.Centre.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Centres.FindAsync(world.Centre.Id).AsTask());
        Assert.NotNull(stored);
        Assert.Equal(CentreStatus.Closed, stored!.Status);
    }

    // ---- Bays ----

    [Fact]
    public async Task CreateBay_ReturnsCreated_AndNormalizesCode()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{world.Centre.Id}/bays", new { code = " b9 ", name = "Bay 9" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("B9", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateBay_WithDuplicateCodeAtSameCentre_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{world.Centre.Id}/bays", new { code = world.Bay.Code.ToLower() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateBay_ForUnknownCentre_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync($"{Url}/{Guid.NewGuid()}/bays", new { code = "B1" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateBay_SetsOutOfService_ButKeepsTheBay()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.DeleteAsync($"{Url}/bays/{world.Bay.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await factory.WithDbAsync(db => db.Bays.FindAsync(world.Bay.Id).AsTask());
        Assert.Equal(BayStatus.OutOfService, stored!.Status);
    }

    [Fact]
    public async Task UpdateBay_ToCodeUsedByAnotherBay_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var second = await TestWorld.AddBayAsync(factory, world.Centre.Id, "B2");
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/bays/{second.Id}", new { code = world.Bay.Code, status = "Available" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
