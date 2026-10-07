using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Auth;

/// <summary>Shared foundation: profile, password, NIC verification, photo upload and admin user management.</summary>
public sealed class AccountManagementTests
{
    private const string Password = "Passw0rd!";
    private const string Url = "/api/v1/auth";

    // ---- own account ----

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_ReplacesThePassword()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "pw@test.lk", UserRole.Commuter);

        var response = await client.PostAsJsonAsync($"{Url}/change-password", new { currentPassword = Password, newPassword = "NewPassw0rd!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var anon = AuthenticationTests.NewClient(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync($"{Url}/login", new { email = "pw@test.lk", password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.PostAsJsonAsync($"{Url}/login", new { email = "pw@test.lk", password = "NewPassw0rd!" })).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_IsRejected()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "pw@test.lk", UserRole.Commuter);

        var response = await client.PostAsJsonAsync($"{Url}/change-password", new { currentPassword = "Wrong123!", newPassword = "NewPassw0rd!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ToATooShortPassword_IsRejected()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "pw@test.lk", UserRole.Commuter);

        var response = await client.PostAsJsonAsync($"{Url}/change-password", new { currentPassword = Password, newPassword = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndDetails_AndKeepsThePassengerProfileInSync()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var anon = AuthenticationTests.NewClient(factory);
        var register = await anon.PostAsJsonAsync($"{Url}/register", new { name = "Old Name", email = "p@test.lk", password = Password });
        var token = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        using var client = AuthenticationTests.NewClient(factory);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PatchAsJsonAsync($"{Url}/me", new { name = "  New Name ", homeLocation = "Kandy", gender = "Female" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Name", body.GetProperty("name").GetString());
        Assert.Equal("Kandy", body.GetProperty("homeLocation").GetString());
        Assert.Equal("New Name", (await factory.WithDbAsync(db => db.Passengers.SingleAsync())).FullName);
    }

    [Fact]
    public async Task UpdateProfile_WithBlankName_IsRejected()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "p@test.lk", UserRole.Commuter);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"{Url}/me", new { name = "  " })).StatusCode);
    }

    [Theory]
    [InlineData("123456789V", "Verified")]
    [InlineData("123456789x", "Verified")]
    [InlineData("200012345678", "Verified")]
    [InlineData("12345", "Invalid")]
    [InlineData("1234567890123", "Invalid")]
    [InlineData("ABCDEFGHIV", "Invalid")]
    public async Task VerifyNic_ChecksTheSriLankanNicFormat(string nic, string expectedStatus)
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "n@test.lk", UserRole.Commuter);

        var response = await client.PostAsJsonAsync($"{Url}/me/verify-nic", new { nicNumber = nic });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, body.GetProperty("nicVerificationStatus").GetString());
        Assert.Equal(nic.ToUpperInvariant(), body.GetProperty("nicNumber").GetString());
    }

    [Fact]
    public async Task ProfilePhoto_Upload_StoresTheImageAndSavesItsUrl()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "ph@test.lk", UserRole.Commuter);
        using var form = new MultipartFormDataContent { { Image([1, 2, 3, 4]), "file", "me.png" } };

        var response = await client.PostAsync($"{Url}/me/profile-photo", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(factory.ImageStorage.Uploads);
        Assert.Equal(4, factory.ImageStorage.Uploads[0].Length);
        var me = await client.GetFromJsonAsync<JsonElement>($"{Url}/me");
        Assert.StartsWith("https://storage.test/profile/", me.GetProperty("profilePhotoUrl").GetString());
    }

    [Fact]
    public async Task ProfilePhoto_Empty_IsRejected_WithoutCallingStorage()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "ph@test.lk", UserRole.Commuter);
        using var form = new MultipartFormDataContent { { Image([]), "file", "empty.png" } };

        var response = await client.PostAsync($"{Url}/me/profile-photo", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.ImageStorage.Uploads);
    }

    [Theory]
    [MemberData(nameof(StorageFailures))]
    public async Task ProfilePhoto_StorageProblems_MapToTheRightHttpStatus(Exception failure, HttpStatusCode expected)
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        factory.ImageStorage.FailWith = failure;
        using var client = await AuthenticationTests.LoggedInAsync(factory, "ph@test.lk", UserRole.Commuter);
        using var form = new MultipartFormDataContent { { Image([1, 2, 3]), "file", "me.png" } };

        var response = await client.PostAsync($"{Url}/me/profile-photo", form);

        Assert.Equal(expected, response.StatusCode);
    }

    public static TheoryData<Exception, HttpStatusCode> StorageFailures() => new()
    {
        { new InvalidDataException("Only PNG and JPEG images are accepted."), HttpStatusCode.BadRequest },
        { new InvalidOperationException("Supabase is not configured."), HttpStatusCode.ServiceUnavailable }
    };

    [Fact]
    public async Task ProfilePhoto_WithoutLogin_ReturnsUnauthorized()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = AuthenticationTests.NewClient(factory);
        using var form = new MultipartFormDataContent { { Image([1]), "file", "me.png" } };

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"{Url}/me/profile-photo", form)).StatusCode);
    }

    // ---- administration ----

    [Fact]
    public async Task CreateUser_AsAdmin_CreatesAStaffAccountThatCanLogIn()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);
        var centre = (await TestWorld.SeedAsync(factory)).Centre;

        var response = await admin.PostAsJsonAsync($"{Url}/create-user", new { name = "Dina", email = "dina@test.lk", password = Password, role = "dispatcher", centreId = centre.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Dispatcher", body.GetProperty("role").GetString());
        using var anon = AuthenticationTests.NewClient(factory);
        Assert.Equal(HttpStatusCode.OK, (await anon.PostAsJsonAsync($"{Url}/login", new { email = "dina@test.lk", password = Password })).StatusCode);
    }

    [Theory]
    [InlineData("Commuter")]
    [InlineData("Driver")]
    [InlineData("CentreManager")]
    [InlineData("FleetOfficer")]
    [InlineData("Dispatcher")]
    public async Task CreateUser_AsNonAdmin_IsForbidden(string role)
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await AuthenticationTests.LoggedInAsync(factory, "x@test.lk", Enum.Parse<UserRole>(role));

        var response = await client.PostAsJsonAsync($"{Url}/create-user", new { name = "N", email = "n@test.lk", password = Password, role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Wizard", "Invalid role")]
    [InlineData("Driver", "driver accounts")]
    public async Task CreateUser_WithInvalidOrReservedRole_IsRejected(string role, string messagePart)
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);

        var response = await admin.PostAsJsonAsync($"{Url}/create-user", new { name = "N", email = "n@test.lk", password = Password, role });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(messagePart, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmailOrWeakPassword_IsRejected()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);

        var duplicate = await admin.PostAsJsonAsync($"{Url}/create-user", new { name = "N", email = "adm@test.lk", password = Password, role = "Dispatcher" });
        var weak = await admin.PostAsJsonAsync($"{Url}/create-user", new { name = "N", email = "w@test.lk", password = "123", role = "Dispatcher" });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [Fact]
    public async Task Users_ListsAccounts_AndStaffOnlyHidesCommutersAndAdmins()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);
        await factory.SeedUserAsync("com@test.lk", Password, UserRole.Commuter);
        await factory.SeedUserAsync("dis@test.lk", Password, UserRole.Dispatcher);

        var all = await admin.GetFromJsonAsync<JsonElement>($"{Url}/users");
        var staff = await admin.GetFromJsonAsync<JsonElement>($"{Url}/users?staffOnly=true");

        Assert.Equal(3, all.GetArrayLength());
        Assert.Equal(1, staff.GetArrayLength());
        Assert.Equal("dis@test.lk", staff[0].GetProperty("email").GetString());
    }

    [Fact]
    public async Task AssignCentre_ToStaff_Works_AndRejectsAdminsCommutersAndUnknownCentres()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);
        var centre = (await TestWorld.SeedAsync(factory)).Centre;
        var staff = await factory.SeedUserAsync("dis@test.lk", Password, UserRole.Dispatcher);
        var commuter = await factory.SeedUserAsync("com@test.lk", Password, UserRole.Commuter);

        var ok = await admin.PatchAsJsonAsync($"{Url}/users/{staff}/centre", new { centreId = centre.Id });
        var forCommuter = await admin.PatchAsJsonAsync($"{Url}/users/{commuter}/centre", new { centreId = centre.Id });
        var unknownCentre = await admin.PatchAsJsonAsync($"{Url}/users/{staff}/centre", new { centreId = Guid.NewGuid() });
        var unknownUser = await admin.PatchAsJsonAsync($"{Url}/users/{Guid.NewGuid()}/centre", new { centreId = centre.Id });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(centre.Id, (await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Id == staff))).CentreId);
        Assert.Equal(HttpStatusCode.BadRequest, forCommuter.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownCentre.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
    }

    [Fact]
    public async Task UpdateUserDetails_ChangesNameAndEmail_AndRejectsDuplicatesAndBlanks()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);
        var user = await factory.SeedUserAsync("old@test.lk", Password, UserRole.Dispatcher);

        var ok = await admin.PatchAsJsonAsync($"{Url}/users/{user}", new { name = "New", email = "new@test.lk" });
        var duplicate = await admin.PatchAsJsonAsync($"{Url}/users/{user}", new { name = "New", email = "adm@test.lk" });
        var blank = await admin.PatchAsJsonAsync($"{Url}/users/{user}", new { name = " ", email = "x@test.lk" });
        var unknown = await admin.PatchAsJsonAsync($"{Url}/users/{Guid.NewGuid()}", new { name = "A", email = "a@test.lk" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        using var anon = AuthenticationTests.NewClient(factory);
        Assert.Equal(HttpStatusCode.OK, (await anon.PostAsJsonAsync($"{Url}/login", new { email = "new@test.lk", password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task SetStatus_Disabling_BlocksLogin_AndEnablingRestoresIt()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);
        var user = await factory.SeedUserAsync("u@test.lk", Password, UserRole.Dispatcher);
        using var anon = AuthenticationTests.NewClient(factory);

        await admin.PatchAsJsonAsync($"{Url}/users/{user}/status", false);
        var blocked = await anon.PostAsJsonAsync($"{Url}/login", new { email = "u@test.lk", password = Password });
        await admin.PatchAsJsonAsync($"{Url}/users/{user}/status", true);
        var restored = await anon.PostAsJsonAsync($"{Url}/login", new { email = "u@test.lk", password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ByAdmin_ReplacesThePassword_AndRejectsBlank()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var admin = await AuthenticationTests.LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);
        var user = await factory.SeedUserAsync("u@test.lk", Password, UserRole.Dispatcher);
        using var anon = AuthenticationTests.NewClient(factory);

        var blank = await admin.PostAsJsonAsync($"{Url}/users/{user}/reset-password", " ");
        var ok = await admin.PostAsJsonAsync($"{Url}/users/{user}/reset-password", "Reset1234!");
        var unknown = await admin.PostAsJsonAsync($"{Url}/users/{Guid.NewGuid()}/reset-password", "Reset1234!");

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync($"{Url}/login", new { email = "u@test.lk", password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anon.PostAsJsonAsync($"{Url}/login", new { email = "u@test.lk", password = "Reset1234!" })).StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_AreForbiddenForAManager()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var manager = await AuthenticationTests.LoggedInAsync(factory, "mgr@test.lk", UserRole.CentreManager);
        var other = await factory.SeedUserAsync("u@test.lk", Password, UserRole.Dispatcher);

        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PatchAsJsonAsync($"{Url}/users/{other}/status", false)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"{Url}/users/{other}/reset-password", "Reset1234!")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PatchAsJsonAsync($"{Url}/users/{other}/centre", new { centreId = (Guid?)null })).StatusCode);
    }

    private static ByteArrayContent Image(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new("image/png");
        return content;
    }
}
