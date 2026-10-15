using api.Common.Constants;
using api.Entities;
using api.Interfaces.Services;

namespace api.Services;

public sealed class AuthCookieService(
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment,
    JwtTokenService jwtService) : IAuthCookieService
{
    private static readonly TimeSpan AccessLifetime = TimeSpan.FromHours(8);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    private HttpContext Http => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("Auth cookies can only be used during an HTTP request.");

    public void SetSignInCookies(User user)
    {
        var cookies = Http.Response.Cookies;
        cookies.Append(AuthCookies.AccessToken, jwtService.GenerateToken(user), CreateOptions(DateTimeOffset.UtcNow.Add(AccessLifetime)));
        cookies.Append(AuthCookies.RefreshToken, jwtService.GenerateToken(user, RefreshLifetime), CreateOptions(DateTimeOffset.UtcNow.Add(RefreshLifetime)));
    }

    public void ClearSignInCookies()
    {
        var options = CreateOptions();
        Http.Response.Cookies.Delete(AuthCookies.AccessToken, options);
        Http.Response.Cookies.Delete(AuthCookies.RefreshToken, options);
    }

    public string? ReadRefreshToken() => Http.Request.Cookies[AuthCookies.RefreshToken];

    // The live web app is on another domain and needs SameSite=None over HTTPS. Local HTTP uses Lax.
    private CookieOptions CreateOptions(DateTimeOffset? expires = null)
    {
        var useCrossSiteCookie = !environment.IsDevelopment() || Http.Request.IsHttps;
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = useCrossSiteCookie,
            SameSite = useCrossSiteCookie ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = expires
        };
    }
}
