using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests.Auth;

/// <summary>Shared foundation: register, login, token validation, refresh and logout through the real JWT stack.</summary>
public sealed class AuthenticationTests
{
    private const string Password = "Passw0rd!";
    private const string Url = "/api/v1/auth";

    private static async Task<(string Token, JsonElement Body)> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync($"{Url}/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("token").GetString()!, body);
    }

    internal static HttpClient NewClient(RealAuthApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = false });

    internal static async Task<HttpClient> LoggedInAsync(RealAuthApiFactory factory, string email, UserRole role, Guid? centreId = null)
    {
        await factory.SeedUserAsync(email, Password, role, centreId);
        var client = NewClient(factory);
        var (token, _) = await LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string? Cookie(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Select(v => v.Split(';')[0]).FirstOrDefault(v => v.StartsWith(name + "="))?[(name.Length + 1)..]
            : null;

    // ---- register ----

    [Fact]
    public async Task Register_CreatesCommuterWithPassengerProfileAndWallet_AndReturnsAToken()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = NewClient(factory);

        var response = await client.PostAsJsonAsync($"{Url}/register", new { name = "Nimal", email = "nimal@test.lk", password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Commuter", body.GetProperty("role").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.Equal(1, await factory.CountAsync<Passenger>());
        Assert.Equal(1, await factory.CountAsync<Wallet>());
        Assert.NotNull(Cookie(response, "upts_access_token"));
        Assert.NotNull(Cookie(response, "upts_refresh_token"));
    }

    [Fact]
    public async Task Register_WithExistingEmail_IsRejected()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("dup@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);

        var response = await client.PostAsJsonAsync($"{Url}/register", new { name = "Dup", email = "DUP@test.lk", password = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public async Task Register_WithTooShortPassword_IsRejected(string password)
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = NewClient(factory);

        var response = await client.PostAsJsonAsync($"{Url}/register", new { name = "A", email = "a@test.lk", password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.CountAsync<Passenger>());
    }

    // ---- login ----

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenWithRoleAndCentreClaims()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        var centreId = (await TestWorld.SeedAsync(factory)).Centre.Id;
        await factory.SeedUserAsync("mgr@test.lk", Password, UserRole.CentreManager, centreId);
        using var client = NewClient(factory);

        var (token, body) = await LoginAsync(client, "mgr@test.lk");

        Assert.Equal("CentreManager", body.GetProperty("role").GetString());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(RealAuthApiFactory.Issuer, jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("/role") && c.Value == "CentreManager");
        Assert.Contains(jwt.Claims, c => c.Type == "centre_id" && c.Value == centreId.ToString());
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddHours(7) && jwt.ValidTo <= DateTime.UtcNow.AddHours(8).AddMinutes(1));
    }

    [Fact]
    public async Task Login_SetsHttpOnlyAccessAndRefreshCookies()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("c@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);

        var response = await client.PostAsJsonAsync($"{Url}/login", new { email = "c@test.lk", password = Password });

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, cookie => Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase));
        Assert.All(cookies, cookie => Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized_AndNoToken()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("c@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);

        var response = await client.PostAsJsonAsync($"{Url}/login", new { email = "c@test.lk", password = "WrongPass1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(Cookie(response, "upts_access_token"));
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("c@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);

        var unknown = await client.PostAsJsonAsync($"{Url}/login", new { email = "nobody@test.lk", password = Password });
        var wrong = await client.PostAsJsonAsync($"{Url}/login", new { email = "c@test.lk", password = "WrongPass1!" });

        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync()); // no user enumeration
    }

    [Fact]
    public async Task Login_ForDisabledAccount_ReturnsForbidden()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("off@test.lk", Password, UserRole.Commuter, isActive: false);
        using var client = NewClient(factory);

        var response = await client.PostAsJsonAsync($"{Url}/login", new { email = "off@test.lk", password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- token validation ----

    [Fact]
    public async Task Me_WithValidBearerToken_ReturnsTheUser()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = await LoggedInAsync(factory, "me@test.lk", UserRole.Dispatcher);

        var response = await client.GetAsync($"{Url}/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("me@test.lk", body.GetProperty("email").GetString());
        Assert.Equal("Dispatcher", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Me_WithTheAccessTokenCookie_Works_ForTheWebApp()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("web@test.lk", Password, UserRole.Admin);
        using var client = NewClient(factory);
        var login = await client.PostAsJsonAsync($"{Url}/login", new { email = "web@test.lk", password = Password });
        var cookie = Cookie(login, "upts_access_token");

        var request = new HttpRequestMessage(HttpMethod.Get, $"{Url}/me");
        request.Headers.Add("Cookie", $"upts_access_token={cookie}");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = NewClient(factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Url}/me")).StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_ReturnsUnauthorized()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("t@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);
        var (token, _) = await LoginAsync(client, "t@test.lk");
        var parts = token.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.{new string(parts[2].Reverse().ToArray())}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Url}/me")).StatusCode);
    }

    [Fact]
    public async Task Me_WithTokenSignedWithAnotherKey_ReturnsUnauthorized()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = NewClient(factory);
        var forged = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            RealAuthApiFactory.Issuer, RealAuthApiFactory.Audience,
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
             new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("another-key-another-key-another-key-another-key-1")),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Url}/me")).StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        var userId = await factory.SeedUserAsync("exp@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<User>>().FindByIdAsync(userId.ToString());
        var token = scope.ServiceProvider.GetRequiredService<api.Services.JwtTokenService>().GenerateToken(user!, TimeSpan.FromMinutes(-10));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Url}/me")).StatusCode);
    }

    [Fact]
    public async Task RealJwt_EnforcesRoles_OnAStaffEndpoint()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var commuter = await LoggedInAsync(factory, "com@test.lk", UserRole.Commuter);
        using var admin = await LoggedInAsync(factory, "adm@test.lk", UserRole.Admin);

        Assert.Equal(HttpStatusCode.Forbidden, (await commuter.GetAsync($"{Url}/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Url}/users")).StatusCode);
    }

    // ---- refresh and logout ----

    [Fact]
    public async Task Refresh_WithValidRefreshCookie_IssuesNewCookies()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("r@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);
        var login = await client.PostAsJsonAsync($"{Url}/login", new { email = "r@test.lk", password = Password });
        var refresh = Cookie(login, "upts_refresh_token");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}/refresh");
        request.Headers.Add("Cookie", $"upts_refresh_token={refresh}");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(Cookie(response, "upts_access_token"));
    }

    [Fact]
    public async Task Refresh_WithoutOrWithGarbageCookie_ReturnsUnauthorized()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = NewClient(factory);

        var none = await client.PostAsync($"{Url}/refresh", null);
        var garbage = new HttpRequestMessage(HttpMethod.Post, $"{Url}/refresh");
        garbage.Headers.Add("Cookie", "upts_refresh_token=not-a-token");

        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(garbage)).StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterAccountIsDisabled_ReturnsUnauthorized()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        var id = await factory.SeedUserAsync("d@test.lk", Password, UserRole.Commuter);
        using var client = NewClient(factory);
        var login = await client.PostAsJsonAsync($"{Url}/login", new { email = "d@test.lk", password = Password });
        var refresh = Cookie(login, "upts_refresh_token");
        await factory.WithDbAsync(async db =>
        {
            (await db.Users.SingleAsync(u => u.Id == id)).IsActive = false;
            await db.SaveChangesAsync();
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}/refresh");
        request.Headers.Add("Cookie", $"upts_refresh_token={refresh}");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Logout_ExpiresBothCookies()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        using var client = NewClient(factory);

        var response = await client.PostAsync($"{Url}/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("upts_access_token=;") || c.StartsWith("upts_access_token=; "));
        Assert.Contains(cookies, c => c.StartsWith("upts_refresh_token=;") || c.StartsWith("upts_refresh_token=; "));
    }
}
