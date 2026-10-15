using api.Data;
using api.Data.Seed;
using api.Hubs;
using api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Common.Extensions;

public static class WebApplicationExtensions
{
    // Applies pending migrations and creates the first admin account.
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await AdminSeeder.SeedAsync(userManager, app.Configuration);
    }

    // The order matters: errors first, then CORS, then auth, then the endpoints.
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseCors(ApiServiceExtensions.WebAppCorsPolicy);
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<NotificationHub>(NotificationHub.Path);
        app.MapHealthChecks("/health");

        return app;
    }
}
