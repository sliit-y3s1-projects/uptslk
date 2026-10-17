using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using api.Common.Constants;
using api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace api.Common.Extensions;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents { OnMessageReceived = ReadTokenFromQueryOrCookie };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.AgentRecoveryStart, context => PerUserPerMinute(context, 3));
            options.AddPolicy(RateLimitPolicies.AgentRecoveryApproval, context => PerUserPerMinute(context, 10));
            options.AddPolicy(RateLimitPolicies.AgentRecoveryCopilot, context => PerUserPerMinute(context, 30));
        });

        return services;
    }

    // SignalR sends the token in the query string. The web app sends it in a cookie.
    private static Task ReadTokenFromQueryOrCookie(MessageReceivedContext context)
    {
        if (string.IsNullOrEmpty(context.Token) && context.Request.Path.StartsWithSegments(NotificationHub.Path))
            context.Token = context.Request.Query["access_token"];
        if (string.IsNullOrEmpty(context.Token))
            context.Token = context.Request.Cookies[AuthCookies.AccessToken];
        return Task.CompletedTask;
    }

    private static RateLimitPartition<string> PerUserPerMinute(HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });

    // Signed-in users are limited per user. Anonymous callers are limited per IP address.
    private static string GetPartitionKey(HttpContext context) =>
        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
