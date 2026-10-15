using api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests.Shared;

/// <summary>Helpers shared by every feature test folder (Centres, Fleet, Dispatch, Bookings).</summary>
internal static class TestApiExtensions
{
    public static async Task<TestApiFactory> CreateInitializedFactoryAsync()
    {
        var factory = new TestApiFactory();
        await factory.InitializeDatabaseAsync();
        return factory;
    }

    /// <summary>A client with no credentials, used for authentication tests.</summary>
    public static HttpClient CreateAnonymousClient(this WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    /// <summary>A client that the test authentication handler treats as the given role.</summary>
    public static HttpClient CreateClientAs(this WebApplicationFactory<Program> factory, string role, Guid? userId = null, Guid? centreId = null)
    {
        var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", (userId ?? Guid.NewGuid()).ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (centreId.HasValue) client.DefaultRequestHeaders.Add("X-Test-Centre-Id", centreId.Value.ToString());
        return client;
    }

    public static async Task WithDbAsync(this WebApplicationFactory<Program> factory, Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public static async Task<T> WithDbAsync<T>(this WebApplicationFactory<Program> factory, Func<AppDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public static Task<int> CountAsync<T>(this WebApplicationFactory<Program> factory) where T : class =>
        factory.WithDbAsync(db => db.Set<T>().CountAsync());
}
