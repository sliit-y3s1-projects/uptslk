using System.Text.Json.Serialization;
using api.Common.Filters;
using api.Common.Middleware;
using api.Data;

namespace api.Common.Extensions;

public static class ApiServiceExtensions
{
    public const string WebAppCorsPolicy = "AllowWebApp";

    // Controllers, JSON, SignalR, Swagger, health checks, error handling and CORS.
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => options.Filters.Add<SendNotificationsFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCors(options => options.AddPolicy(WebAppCorsPolicy, policy => policy
            .WithOrigins(GetWebAppOrigins(configuration))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return services;
    }

    private static string[] GetWebAppOrigins(IConfiguration configuration) =>
        new[]
        {
            "http://localhost:5173",
            "http://localhost:4173",
            "https://uptslk.vercel.app",
            configuration["Payments:Stripe:WebAppBaseUrl"]
        }
        .OfType<string>()
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
