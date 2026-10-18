using System.Net;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Auth;

/// <summary>Security headers found missing by the OWASP ZAP scan (ZAP-1: X-Content-Type-Options).</summary>
public sealed class SecurityHeaderTests
{
    [Theory]
    [InlineData("/api/v1/centres", HttpStatusCode.OK)]
    [InlineData("/api/v1/passengers", HttpStatusCode.Unauthorized)]
    [InlineData("/api/v1/centres/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound)]
    public async Task EveryResponse_TellsBrowsersNotToGuessTheContentType(string path, HttpStatusCode expected)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }
}
