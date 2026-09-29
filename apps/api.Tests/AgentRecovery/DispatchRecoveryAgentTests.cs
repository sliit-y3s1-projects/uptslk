using api.Models;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Agents;
using api.Services.AgentRecovery.Tools;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class DispatchRecoveryAgentTests
{
    [Fact]
    public async Task AnalyseAsync_UsesNetworkAndFleetEvidenceForDriverSearch()
    {
        var centreId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var bayId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddMinutes(15);
        var tools = new RecordingDispatchTools();
        var agent = new DispatchRecoveryAgent(tools);
        var context = CreateContext(
            centreId,
            driverId,
            new Dictionary<RecoveryAgentId, AgentRecommendation>
            {
                [RecoveryAgentId.NetworkContinuity] = new(
                    "Network Continuity Agent",
                    "Network recommendation",
                    [],
                    [],
                    BayId: bayId,
                    ScheduledTime: scheduledTime),
                [RecoveryAgentId.FleetReadiness] = new(
                    "Fleet Readiness Agent",
                    "Fleet recommendation",
                    [],
                    [],
                    VehicleId: vehicleId,
                    Capacity: 50)
            });

        var result = await agent.AnalyseAsync(context, CancellationToken.None);

        Assert.NotNull(tools.LastInput);
        Assert.Equal(centreId, tools.LastInput.CentreId);
        Assert.Equal(driverId, tools.LastInput.ExcludedDriverId);
        Assert.Equal(vehicleId, tools.LastInput.VehicleId);
        Assert.Equal(bayId, tools.LastInput.BayId);
        Assert.Equal(scheduledTime, tools.LastInput.ScheduledTime);
        Assert.Equal(tools.DriverId, result.Recommendation.DriverId);
    }

    [Fact]
    public async Task AnalyseAsync_RejectsMissingDependencyEvidence()
    {
        var agent = new DispatchRecoveryAgent(new RecordingDispatchTools());
        var context = CreateContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Dictionary<RecoveryAgentId, AgentRecommendation>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.AnalyseAsync(context, CancellationToken.None));

        Assert.Contains("missing required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RecoveryContext CreateContext(
        Guid centreId,
        Guid driverId,
        IReadOnlyDictionary<RecoveryAgentId, AgentRecommendation> evidence)
    {
        var trip = new Trip
        {
            CentreId = centreId,
            DriverId = driverId,
            VehicleId = Guid.NewGuid(),
            BayId = Guid.NewGuid(),
            ScheduledTime = DateTime.UtcNow,
            Route = new Route
            {
                RouteNumber = "EX-01",
                Name = "Example route",
                Origin = "Origin",
                Destination = "Destination",
                EstimatedDurationMin = 60
            }
        };

        return new RecoveryContext(new AgentWorkflow(), trip, 20, evidence);
    }

    private sealed class RecordingDispatchTools : IDispatchRecoveryTools
    {
        public Guid DriverId { get; } = Guid.NewGuid();
        public FindConflictFreeDriverInput? LastInput { get; private set; }

        public Task<FindConflictFreeDriverOutput> FindConflictFreeDriverAsync(
            FindConflictFreeDriverInput input,
            CancellationToken cancellationToken)
        {
            LastInput = input;
            return Task.FromResult(new FindConflictFreeDriverOutput(DriverId, "Test Driver", 1));
        }
    }
}
