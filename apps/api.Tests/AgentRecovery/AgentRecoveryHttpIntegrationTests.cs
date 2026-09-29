using System.Net;
using System.Net.Http.Json;
using api.Data;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class AgentRecoveryHttpIntegrationTests
{
    [Fact]
    public async Task Workflows_RejectsUnauthenticatedRequest()
    {
        using var factory = await CreateFactoryAsync();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/v1/agent-recovery/workflows");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Workflows_ForbidsCentreManagerFromRequestingAnotherCentre()
    {
        using var factory = await CreateFactoryAsync();
        using var client = CreateClient(factory);
        var ownCentreId = Guid.NewGuid();
        var otherCentreId = Guid.NewGuid();
        Authenticate(client, "CentreManager", ownCentreId);

        var response = await client.GetAsync(
            $"/api/v1/agent-recovery/workflows?centreId={otherCentreId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Start_ReturnsNotFoundForIncidentOwnedByAnotherCentre()
    {
        using var factory = await CreateFactoryAsync();
        var ownCentreId = Guid.NewGuid();
        var otherCentre = new Centre
        {
            Code = "OTHER",
            Name = "Other Centre",
            City = "Colombo",
            District = "Colombo",
            Status = CentreStatus.Operating
        };
        var incident = new Incident
        {
            CentreId = otherCentre.Id,
            Centre = otherCentre,
            ReportedByName = "Test operator",
            Type = IncidentType.Breakdown,
            Severity = IncidentSeverity.Medium,
            Title = "Foreign incident",
            Description = "This incident belongs to another centre.",
            Status = IncidentStatus.Open,
            SlaDueAt = DateTime.UtcNow.AddHours(1)
        };
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(otherCentre, incident);
            await db.SaveChangesAsync();
        }
        using var client = CreateClient(factory);
        Authenticate(client, "CentreManager", ownCentreId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/agent-recovery/workflows",
            new { incidentId = incident.Id, objective = "Restore service safely." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Start_ReturnsTooManyRequestsAfterPerUserLimitIsExceeded()
    {
        using var factory = await CreateFactoryAsync();
        using var client = CreateClient(factory);
        Authenticate(client, "Admin", null);
        var request = new
        {
            incidentId = Guid.NewGuid(),
            objective = "Restore service safely."
        };

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var acceptedByLimiter = await client.PostAsJsonAsync(
                "/api/v1/agent-recovery/workflows",
                request);
            Assert.Equal(HttpStatusCode.BadRequest, acceptedByLimiter.StatusCode);
        }

        var rejectedByLimiter = await client.PostAsJsonAsync(
            "/api/v1/agent-recovery/workflows",
            request);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedByLimiter.StatusCode);
    }

    private static async Task<RecoveryApiFactory> CreateFactoryAsync()
    {
        var factory = new RecoveryApiFactory();
        await factory.InitializeDatabaseAsync();
        return factory;
    }

    private static HttpClient CreateClient(RecoveryApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private static void Authenticate(HttpClient client, string role, Guid? centreId)
    {
        client.DefaultRequestHeaders.Add("X-Test-User-Id", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        if (centreId.HasValue)
            client.DefaultRequestHeaders.Add("X-Test-Centre-Id", centreId.Value.ToString());
    }
}
