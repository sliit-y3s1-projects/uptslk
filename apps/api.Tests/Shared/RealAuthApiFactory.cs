using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using api.Enums;

namespace api.Tests.Shared;

/// <summary>
/// The API with its REAL JWT / cookie authentication (no header-based test handler), on in-memory SQLite.
/// Used to test login, token validation, refresh and role enforcement end to end.
/// </summary>
internal sealed class RealAuthApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "test-signing-key-test-signing-key-test-signing-key-1234";
    public const string Issuer = "upts-test";
    public const string Audience = "upts-test-web";

    static RealAuthApiFactory()
    {
        // Program.cs reads the JWT settings while it builds the host, so they are provided as environment variables.
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
    }

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public FakeImageStorage ImageStorage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            services.RemoveAll<IImageStorageService>();
            services.AddSingleton<IImageStorageService>(ImageStorage);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await _connection.OpenAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    }

    public static async Task<RealAuthApiFactory> CreateAsync()
    {
        var factory = new RealAuthApiFactory();
        await factory.InitializeDatabaseAsync();
        return factory;
    }

    /// <summary>Creates a user directly through Identity (bypassing the API) and returns its id.</summary>
    public async Task<Guid> SeedUserAsync(
        string email, string password, UserRole role, Guid? centreId = null, bool isActive = true, string name = "Test User")
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User { UserName = email, Email = email, Name = name, Role = role, CentreId = centreId, IsActive = isActive };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

/// <summary>Image storage replacement that records uploads and can be told to fail.</summary>
internal sealed class FakeImageStorage : IImageStorageService
{
    public Exception? FailWith { get; set; }
    public List<(Guid Id, string ContentType, int Length)> Uploads { get; } = [];

    public Task<string> UploadProfileAsync(Guid userId, byte[] content, string contentType, CancellationToken cancellationToken = default) =>
        Record(userId, content, contentType, "profile");

    public Task<string> UploadVehicleAsync(Guid vehicleId, byte[] content, string contentType, CancellationToken cancellationToken = default) =>
        Record(vehicleId, content, contentType, "vehicle");

    private Task<string> Record(Guid id, byte[] content, string contentType, string kind)
    {
        if (FailWith is not null) throw FailWith;
        Uploads.Add((id, contentType, content.Length));
        return Task.FromResult($"https://storage.test/{kind}/{id}.png");
    }
}
