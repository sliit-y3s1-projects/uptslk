using api.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://0.0.0.0:5250");

builder.Services
    .AddApiServices(builder.Configuration)
    .AddDatabase(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddRateLimitPolicies()
    .AddApplicationServices()
    .AddAgentRecovery(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    await app.InitializeDatabaseAsync();

app.UseApiPipeline();
app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program { }
