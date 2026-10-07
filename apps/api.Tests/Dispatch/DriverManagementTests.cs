using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C: driver accounts (create/update/deactivate) with centre-level authorization.</summary>
public sealed class DriverManagementTests
{
    private const string Url = "/api/v1/drivers";

    private static object NewDriver(Guid centreId, string email = "Driver.One@Test.lk", string license = " dl-100 ", string password = "Passw0rd!", string status = "Active") => new
    {
        centreId, fullName = " Kamal Perera ", email, password, phoneNumber = " 0771234567 ", licenseNumber = license, status
    };

    [Fact]
    public async Task Create_AsManagerOfTheCentre_CreatesDriverAndLinkedLoginAccount()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DL-100", body.GetProperty("licenseNumber").GetString());
        Assert.Equal("driver.one@test.lk", body.GetProperty("email").GetString());
        var user = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == "driver.one@test.lk"));
        Assert.Equal(UserRole.Driver, user.Role);
        Assert.Equal(world.Centre.Id, user.CentreId);
        var driver = await factory.WithDbAsync(db => db.Drivers.SingleAsync(d => d.LicenseNumber == "DL-100"));
        Assert.Equal(user.Id, driver.UserId);
        Assert.Equal("Kamal Perera", driver.FullName);
        Assert.Equal("0771234567", driver.PhoneNumber);
    }

    [Fact]
    public async Task Create_AsAdmin_WorksForAnyCentre()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id))).StatusCode);
    }

    [Fact]
    public async Task Create_AsManagerOfAnotherCentre_IsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("CentreManager", centreId: Guid.NewGuid());

        var response = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, await factory.CountAsync<Driver>()); // only the seeded driver
    }

    [Fact]
    public async Task Create_WithDuplicateLicense_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id, license: world.Driver.LicenseNumber.ToLower()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict_AndCreatesNothing()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id));

        var response = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id, license: "DL-200"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, await factory.CountAsync<Driver>()); // seeded driver + the first new one
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd!")]
    [InlineData("ok@test.lk", "short")]
    public async Task Create_WithInvalidEmailOrPassword_IsRejected(string email, string password)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id, email: email, password: password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForUnknownCentre_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Url, NewDriver(Guid.NewGuid()))).StatusCode);
    }

    [Fact]
    public async Task Create_AsInactive_CreatesADisabledLogin()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id, status: "Inactive"));

        Assert.False((await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Role == UserRole.Driver))).IsActive);
    }

    [Theory]
    [InlineData("Commuter")]
    [InlineData("Dispatcher")]
    [InlineData("FleetOfficer")]
    [InlineData("Driver")]
    public async Task Create_ByOtherRoles_IsForbidden(string role)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs(role, centreId: world.Centre.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id))).StatusCode);
    }

    [Fact]
    public async Task Update_ChangesDriver_AndKeepsTheLoginAccountInSync()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        var created = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new
        {
            centreId = world.Centre.Id, fullName = "Renamed Driver", phoneNumber = "0719999999", status = "Inactive"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var driver = await factory.WithDbAsync(db => db.Drivers.FindAsync(id).AsTask());
        Assert.Equal(DriverStatus.Inactive, driver!.Status);
        var user = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Id == driver.UserId));
        Assert.Equal("Renamed Driver", user.Name);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Update_ByManagerOfAnotherCentre_IsForbidden_AndUnknownDriverIsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var other = factory.CreateClientAs("CentreManager", centreId: Guid.NewGuid());
        using var admin = factory.CreateClientAs("Admin");
        var body = new { centreId = world.Centre.Id, fullName = "X", status = "Active" };

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"{Url}/{world.Driver.Id}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", body)).StatusCode);
    }

    [Fact]
    public async Task Deactivate_DisablesDriverAndLogin_ButKeepsTheRecord()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");
        var created = await client.PostAsJsonAsync(Url, NewDriver(world.Centre.Id));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.DeleteAsync($"{Url}/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var driver = await factory.WithDbAsync(db => db.Drivers.FindAsync(id).AsTask());
        Assert.Equal(DriverStatus.Inactive, driver!.Status);
        Assert.False((await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Id == driver.UserId))).IsActive);
    }

    [Fact]
    public async Task Deactivate_ByManagerOfAnotherCentre_IsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var other = factory.CreateClientAs("CentreManager", centreId: Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"{Url}/{world.Driver.Id}")).StatusCode);
        Assert.Equal(DriverStatus.Active, (await factory.WithDbAsync(db => db.Drivers.FindAsync(world.Driver.Id).AsTask()))!.Status);
    }

    [Fact]
    public async Task List_FiltersByCentreStatusAndSearch_AndGetReturnsTheDriver()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var a = await TestWorld.SeedAsync(factory, "AAA", "AAA-1", "LIC-AAA");
        var b = await TestWorld.SeedAsync(factory, "BBB", "BBB-1", "LIC-BBB");
        await factory.WithDbAsync(async db =>
        {
            (await db.Drivers.FindAsync(b.Driver.Id))!.Status = DriverStatus.Inactive;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Dispatcher");

        var byCentre = await client.GetFromJsonAsync<JsonElement>($"{Url}?centreId={a.Centre.Id}");
        var inactive = await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Inactive");
        var search = await client.GetFromJsonAsync<JsonElement>($"{Url}?search=lic-bbb");
        var one = await client.GetFromJsonAsync<JsonElement>($"{Url}/{a.Driver.Id}");

        Assert.Equal(1, byCentre.GetArrayLength());
        Assert.Equal(b.Driver.Id, inactive[0].GetProperty("id").GetGuid());
        Assert.Equal(1, search.GetArrayLength());
        Assert.Equal("LIC-AAA", one.GetProperty("licenseNumber").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }
}
