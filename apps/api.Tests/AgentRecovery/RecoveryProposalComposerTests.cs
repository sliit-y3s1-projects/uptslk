using api.Models;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryProposalComposerTests
{
    private readonly RecoveryProposalComposer _composer = new();

    [Fact]
    public void Compose_CreatesCompatibleFullRecoveryProposal()
    {
        var trip = CreateTrip(45);
        var replacementVehicleId = Guid.NewGuid();
        var replacementDriverId = Guid.NewGuid();
        var bayId = Guid.NewGuid();
        var recommendations = CreateRecommendations(
            replacementVehicleId,
            replacementDriverId,
            bayId,
            trip.ScheduledTime.AddMinutes(15),
            50);

        var result = _composer.Compose(
            trip,
            40,
            SafeRecoveryPlanFactory.Create("Restore service."),
            recommendations,
            []);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Proposal);
        Assert.Equal(replacementVehicleId, result.Proposal.VehicleId);
        Assert.Equal(replacementDriverId, result.Proposal.DriverId);
        Assert.Equal(bayId, result.Proposal.BayId);
    }

    [Fact]
    public void Compose_AllowsFocusedDelayToRetainCurrentVehicleAndDriver()
    {
        var trip = CreateTrip(45);
        var plan = new RecoveryPlanDraft(
            "Assess a delay.",
            [
                new PlannedRecoveryStep("network-check", 1, RecoveryAgentId.NetworkContinuity, "Assess continuity.", []),
                new PlannedRecoveryStep("passenger-check", 2, RecoveryAgentId.PassengerFareImpact, "Assess passengers.", ["network-check"])
            ],
            "Ready for validation.");
        var recommendations = new Dictionary<RecoveryAgentId, AgentRecommendation>
        {
            [RecoveryAgentId.NetworkContinuity] = Recommendation(
                "Network",
                bayId: trip.BayId,
                scheduledTime: trip.ScheduledTime.AddMinutes(10)),
            [RecoveryAgentId.PassengerFareImpact] = Recommendation("Passenger")
        };

        var result = _composer.Compose(trip, 20, plan, recommendations, []);

        Assert.Empty(result.Errors);
        Assert.Equal(trip.VehicleId, result.Proposal!.VehicleId);
        Assert.Equal(trip.DriverId, result.Proposal.DriverId);
    }

    [Fact]
    public void Compose_RejectsIncompatibleCapacityAndAffectedResources()
    {
        var trip = CreateTrip(45);
        var recommendations = CreateRecommendations(
            trip.VehicleId,
            trip.DriverId,
            Guid.NewGuid(),
            trip.ScheduledTime.AddMinutes(15),
            20);

        var result = _composer.Compose(
            trip,
            40,
            SafeRecoveryPlanFactory.Create("Restore service."),
            recommendations,
            []);

        Assert.Null(result.Proposal);
        Assert.Contains(result.Errors, error => error.Contains("affected vehicle", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("affected driver", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("cannot carry", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(121)]
    public void Compose_RejectsDepartureOutsideRecoveryWindow(int offsetMinutes)
    {
        var trip = CreateTrip(45);
        var recommendations = CreateRecommendations(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            trip.ScheduledTime.AddMinutes(offsetMinutes),
            50);

        var result = _composer.Compose(
            trip,
            20,
            SafeRecoveryPlanFactory.Create("Restore service."),
            recommendations,
            []);

        Assert.Null(result.Proposal);
        Assert.Contains(result.Errors, error => error.Contains("departure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compose_PreservesExecutionErrorsAndRequiresPassengerEvidence()
    {
        var trip = CreateTrip(45);
        var recommendations = CreateRecommendations(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            trip.ScheduledTime.AddMinutes(15),
            50);
        recommendations.Remove(RecoveryAgentId.PassengerFareImpact);

        var result = _composer.Compose(
            trip,
            20,
            SafeRecoveryPlanFactory.Create("Restore service."),
            recommendations,
            ["A controlled tool failed."]);

        Assert.Null(result.Proposal);
        Assert.Contains("A controlled tool failed.", result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("passenger impact", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compose_RejectsDriverCheckedForDifferentResourceCombination()
    {
        var trip = CreateTrip(45);
        var recommendations = CreateRecommendations(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            trip.ScheduledTime.AddMinutes(15),
            50);
        recommendations[RecoveryAgentId.DispatchRecovery] = Recommendation(
            "Dispatch",
            vehicleId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            bayId: Guid.NewGuid(),
            scheduledTime: trip.ScheduledTime.AddMinutes(30));

        var result = _composer.Compose(
            trip,
            20,
            SafeRecoveryPlanFactory.Create("Restore service."),
            recommendations,
            []);

        Assert.Null(result.Proposal);
        Assert.Contains(result.Errors, error => error.Contains("proposed vehicle", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("departure bay", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("departure time", StringComparison.OrdinalIgnoreCase));
    }

    private static Trip CreateTrip(int capacity) => new()
    {
        VehicleId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        BayId = Guid.NewGuid(),
        ScheduledTime = DateTime.UtcNow,
        Vehicle = new Vehicle { Capacity = capacity }
    };

    private static Dictionary<RecoveryAgentId, AgentRecommendation> CreateRecommendations(
        Guid vehicleId,
        Guid driverId,
        Guid bayId,
        DateTime scheduledTime,
        int capacity) => new()
    {
        [RecoveryAgentId.NetworkContinuity] = Recommendation("Network", bayId: bayId, scheduledTime: scheduledTime),
        [RecoveryAgentId.FleetReadiness] = Recommendation("Fleet", vehicleId: vehicleId, capacity: capacity),
        [RecoveryAgentId.DispatchRecovery] = Recommendation(
            "Dispatch",
            vehicleId: vehicleId,
            driverId: driverId,
            bayId: bayId,
            scheduledTime: scheduledTime),
        [RecoveryAgentId.PassengerFareImpact] = Recommendation("Passenger")
    };

    private static AgentRecommendation Recommendation(
        string name,
        Guid? vehicleId = null,
        Guid? driverId = null,
        Guid? bayId = null,
        DateTime? scheduledTime = null,
        int? capacity = null) =>
        new(name, $"{name} recommendation", [], [], vehicleId, driverId, bayId, scheduledTime, capacity);
}
