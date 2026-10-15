using api.Entities;

namespace api.Interfaces.Services;

// Reads and writes the HTTP-only cookies the web app uses to stay signed in.
public interface IAuthCookieService
{
    void SetSignInCookies(User user);
    void ClearSignInCookies();
    string? ReadRefreshToken();
}
