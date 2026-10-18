using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Entities;
using api.Entities.Enums;
using api.Tests.Auth;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: passenger portal accounts, profile updates, restore and staff password resets.</summary>
public sealed class PassengerAccountTests
{
    private const string Url = "/api/v1/passengers";
    private const string Password = "Passw0rd!";
    private const string NewPassword = "N3wPassw0rd!";

    private static async Task<HttpStatusCode> LoginStatusAsync(RealAuthApiFactory factory, string email, string password)
    {
        using var client = AuthenticationTests.NewClient(factory);
        return (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password })).StatusCode;
    }

    private static async Task<Guid> CreatePortalPassengerAsync(HttpClient staff, string email, string phone = "0771234567")
    {
        var response = await staff.PostAsJsonAsync(Url, new { fullName = "Portal Passenger", phoneNumber = phone, email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // ---- create with a portal account ----

    [Fact]
    public async Task Create_WithEmailAndPassword_CreatesACommuterLoginThatWorks()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var staff = await AuthenticationTests.LoggedInAsync(factory, "mgr@test.lk", UserRole.CentreManager);

        var response = await staff.PostAsJsonAsync(Url, new { fullName = " Kamala Perera ", phoneNumber = "0771234567", email = " kamala@test.lk ", password = Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("portalAccountCreated").GetBoolean());
        var passenger = await factory.WithDbAsync(db => db.Passengers.Include(p => p.User).SingleAsync());
        Assert.Equal("Kamala Perera", passenger.FullName);
        Assert.Equal(UserRole.Commuter, passenger.User!.Role);
        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync(factory, "kamala@test.lk", Password));
    }

    [Fact]
    public async Task Create_WithAnEmailThatAlreadyHasAnAccount_ReturnsConflict_AndCreatesNothing()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var staff = await AuthenticationTests.LoggedInAsync(factory, "mgr@test.lk", UserRole.CentreManager);

        var response = await staff.PostAsJsonAsync(Url, new { fullName = "Copy", phoneNumber = "0779999999", email = "mgr@test.lk", password = Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, await factory.CountAsync<Passenger>());
        Assert.Equal(1, await factory.CountAsync<User>());
    }

    [Fact]
    public async Task Create_WithAPasswordShorterThanEight_IsRejected_AndCreatesNoLogin()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var staff = await AuthenticationTests.LoggedInAsync(factory, "mgr@test.lk", UserRole.CentreManager);

        var response = await staff.PostAsJsonAsync(Url, new { fullName = "Short", phoneNumber = "0771111111", email = "short@test.lk", password = "Pa55wd!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountAsync<Passenger>());
        Assert.Equal(1, await factory.CountAsync<User>());
    }

    // ---- update ----

    [Fact]
    public async Task Update_ChangesDetails_NormalizesThePhone_AndTrimsTheName()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 100);
        using var client = factory.CreateClientAs("Dispatcher");

        var response = await client.PutAsJsonAsync($"{Url}/{passenger.Id}",
            new { fullName = "  Sunil Silva ", phoneNumber = "071 555-0000", email = "sunil@test.lk", category = "Senior", isActive = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = await factory.WithDbAsync(db => db.Passengers.SingleAsync());
        Assert.Equal("Sunil Silva", saved.FullName);
        Assert.Equal("0715550000", saved.PhoneNumber);
        Assert.Equal("sunil@test.lk", saved.Email);
        Assert.Equal(PassengerCategory.Senior, saved.Category);
    }

    [Fact]
    public async Task Update_ToAPhoneNumberUsedByAnotherPassenger_ReturnsConflict_AndKeepsTheOldNumber()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (first, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        var (second, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PutAsJsonAsync($"{Url}/{second.Id}",
            new { fullName = "Second", phoneNumber = first.PhoneNumber, category = "Adult", isActive = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var saved = await factory.WithDbAsync(db => db.Passengers.SingleAsync(p => p.Id == second.Id));
        Assert.Equal(second.PhoneNumber, saved.PhoneNumber);
    }

    [Fact]
    public async Task Update_Deactivate_Restore_AndTopUp_OfAnUnknownPassenger_ReturnNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");
        var id = Guid.NewGuid();

        var update = await client.PutAsJsonAsync($"{Url}/{id}", new { fullName = "X", phoneNumber = "0770000000", category = "Adult", isActive = true });
        var deactivate = await client.DeleteAsync($"{Url}/{id}");
        var restore = await client.PostAsync($"{Url}/{id}/restore", null);
        var topUp = await client.PostAsJsonAsync($"{Url}/{id}/wallet/top-ups", new { amount = 100m });

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, restore.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, topUp.StatusCode);
    }

    [Fact]
    public async Task Restore_ReactivatesADisabledPortalLogin()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, userId) = await TestWorld.AddPassengerAsync(factory, world, 0);
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.IsActive = false;
            var stored = await db.Passengers.SingleAsync(p => p.Id == passenger.Id);
            stored.IsActive = false;
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsync($"{Url}/{passenger.Id}/restore", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(await factory.WithDbAsync(db => db.Users.Where(u => u.Id == userId).Select(u => u.IsActive).SingleAsync()));
        Assert.True(await factory.WithDbAsync(db => db.Passengers.Where(p => p.Id == passenger.Id).Select(p => p.IsActive).SingleAsync()));
    }

    // ---- staff password reset ----

    [Fact]
    public async Task ResetPassword_ReplacesThePassengersPassword_SoOnlyTheNewOneLogsIn()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var staff = await AuthenticationTests.LoggedInAsync(factory, "disp@test.lk", UserRole.Dispatcher);
        var passengerId = await CreatePortalPassengerAsync(staff, "reset@test.lk");

        var response = await staff.PostAsJsonAsync($"{Url}/{passengerId}/reset-password", new { password = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await LoginStatusAsync(factory, "reset@test.lk", Password));
        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync(factory, "reset@test.lk", NewPassword));
    }

    [Fact]
    public async Task ResetPassword_ForAPassengerWithoutAPortalAccount_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Url, new { fullName = "Walk In", phoneNumber = "0772222222" });
        var passengerId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync($"{Url}/{passengerId}/reset-password", new { password = NewPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("portal account", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ResetPassword_ForAnUnknownPassenger_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync($"{Url}/{Guid.NewGuid()}/reset-password", new { password = NewPassword });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short1!")]
    public async Task ResetPassword_WithABlankOrTooShortPassword_IsRejected_AndTheOldPasswordStillWorks(string password)
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var staff = await AuthenticationTests.LoggedInAsync(factory, "mgr@test.lk", UserRole.CentreManager);
        var passengerId = await CreatePortalPassengerAsync(staff, "keep@test.lk");

        var response = await staff.PostAsJsonAsync($"{Url}/{passengerId}/reset-password", new { password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync(factory, "keep@test.lk", Password));
    }

    [Fact]
    public async Task ResetPassword_IsForbiddenForACommuterAndAFleetOfficer()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var staff = await AuthenticationTests.LoggedInAsync(factory, "mgr@test.lk", UserRole.CentreManager);
        var passengerId = await CreatePortalPassengerAsync(staff, "target@test.lk");
        using var commuter = await AuthenticationTests.LoggedInAsync(factory, "c@test.lk", UserRole.Commuter);
        using var fleet = await AuthenticationTests.LoggedInAsync(factory, "f@test.lk", UserRole.FleetOfficer);

        var byCommuter = await commuter.PostAsJsonAsync($"{Url}/{passengerId}/reset-password", new { password = NewPassword });
        var byFleet = await fleet.PostAsJsonAsync($"{Url}/{passengerId}/reset-password", new { password = NewPassword });

        Assert.Equal(HttpStatusCode.Forbidden, byCommuter.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byFleet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, await LoginStatusAsync(factory, "target@test.lk", Password));
    }
}
