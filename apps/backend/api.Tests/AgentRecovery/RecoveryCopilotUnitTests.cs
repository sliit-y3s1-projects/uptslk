using api.Entities;
using api.Entities.Enums;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Copilot;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryCopilotUnitTests
{
    [Fact]
    public void Capacity_ScoresZeroWhenTheBusCannotCarryEveryone() =>
        Assert.Equal(0, RecoveryScoring.Capacity(30, 31));

    [Fact]
    public void Capacity_PrefersABusThatFitsOverOneThatWastesSeats()
    {
        var snug = RecoveryScoring.Capacity(45, 40);
        var oversized = RecoveryScoring.Capacity(100, 40);

        Assert.Equal(RecoveryScoring.CapacityMax, snug);
        Assert.True(oversized < snug);
    }

    [Fact]
    public void Capacity_PenalisesAnAlmostFullBus() =>
        Assert.True(RecoveryScoring.Capacity(40, 40) < RecoveryScoring.CapacityMax);

    [Theory]
    [InlineData(0, 25)]
    [InlineData(45, 12.5)]
    [InlineData(90, 0)]
    [InlineData(120, 0)]
    public void Delay_LosesPointsEveryMinute(int delay, double expected) =>
        Assert.Equal(expected, RecoveryScoring.Delay(delay), 3);

    [Theory]
    [InlineData(null, 25)]
    [InlineData(0, 0)]
    [InlineData(30, 12.5)]
    [InlineData(90, 25)]
    [InlineData(-20, 0)]
    public void Slack_RewardsSpareTimeBeforeTheNextDuty(int? slack, double expected) =>
        Assert.Equal(expected, RecoveryScoring.Slack(slack), 3);

    [Fact]
    public void Memory_LowersTheScoreForEveryEarlierRejection()
    {
        Assert.Equal(7, RecoveryScoring.Memory(0, false));
        Assert.Equal(10, RecoveryScoring.Memory(0, true));
        Assert.Equal(-8, RecoveryScoring.Memory(1, false));
        Assert.Equal(-30, RecoveryScoring.Memory(5, false));
    }

    [Fact]
    public void Total_StaysBetweenZeroAndOneHundred()
    {
        Assert.Equal(0, RecoveryScoring.Total(-30, 0, 0, 0));
        Assert.Equal(100, RecoveryScoring.Total(40, 25, 25, 10, 50));
    }

    [Fact]
    public void Interpret_ReadsADriverComplaint()
    {
        var result = RecoveryMemoryService.Interpret("The driver already worked a double shift", 15, "NB-1", "Kamal Perera");

        Assert.True(result.AvoidDriver);
        Assert.False(result.AvoidVehicle);
        Assert.Null(result.MaxDelayMinutes);
    }

    [Fact]
    public void Interpret_ReadsAStatedDelayLimit()
    {
        var result = RecoveryMemoryService.Interpret("30 minutes is too long to make passengers wait", 45, null, null);

        Assert.Equal(30, result.MaxDelayMinutes);
        Assert.False(result.AvoidDriver);
    }

    [Fact]
    public void Interpret_TightensTheDelayWhenTheReasonIsOnlyThatItIsLate()
    {
        var result = RecoveryMemoryService.Interpret("too late", 45, null, null);

        Assert.Equal(30, result.MaxDelayMinutes);
    }

    [Fact]
    public void Interpret_RecognisesAPlateOrDriverNameInTheReason()
    {
        Assert.True(RecoveryMemoryService.Interpret("nb-1234 is in bad shape", 15, "NB-1234", "Kamal Perera").AvoidVehicle);
        Assert.True(RecoveryMemoryService.Interpret("not kamal again", 15, "NB-1234", "Kamal Perera").AvoidDriver);
    }

    [Fact]
    public void Interpret_AvoidsTheRejectedBusAndDriverWhenTheReasonSaysNothingSpecific()
    {
        var result = RecoveryMemoryService.Interpret("not happy", 15, "NB-1", "Kamal Perera");

        Assert.True(result.AvoidVehicle);
        Assert.True(result.AvoidDriver);
        Assert.False(result.AvoidBay);
    }

    [Fact]
    public void Assess_CountsEachRejectionOnceAndRemembersApprovals()
    {
        var vehicle = Guid.NewGuid();
        var driver = Guid.NewGuid();
        var rejected = new RecoveryMemory
        {
            Kind = RecoveryMemoryKind.Rejected, VehicleId = vehicle, DriverId = driver,
            AvoidVehicle = true, AvoidDriver = true, Reason = "Tired driver"
        };
        var approved = new RecoveryMemory { Kind = RecoveryMemoryKind.Approved, VehicleId = vehicle, DriverId = driver };

        var onlyRejected = RecoveryMemoryService.Assess(vehicle, driver, null, 15, [rejected]);
        var unrelated = RecoveryMemoryService.Assess(Guid.NewGuid(), Guid.NewGuid(), null, 15, [rejected]);
        var liked = RecoveryMemoryService.Assess(vehicle, driver, null, 15, [approved]);

        Assert.Equal(1, onlyRejected.Rejections);
        Assert.Contains("Tired driver", onlyRejected.Notes[0]);
        Assert.Equal(0, unrelated.Rejections);
        Assert.True(liked.PreviouslyApproved);
    }

    [Fact]
    public void Assess_AppliesAnEarlierDelayCap()
    {
        var cap = new RecoveryMemory { Kind = RecoveryMemoryKind.Rejected, MaxDelayMinutes = 20 };

        Assert.Equal(1, RecoveryMemoryService.Assess(Guid.NewGuid(), null, null, 30, [cap]).Rejections);
        Assert.Equal(0, RecoveryMemoryService.Assess(Guid.NewGuid(), null, null, 15, [cap]).Rejections);
    }

    [Theory]
    [InlineData(PassengerCategory.Senior, false, RecoveryMessageSegments.Priority)]
    [InlineData(PassengerCategory.Child, true, RecoveryMessageSegments.Priority)]
    [InlineData(PassengerCategory.Adult, true, RecoveryMessageSegments.App)]
    [InlineData(PassengerCategory.Student, false, RecoveryMessageSegments.NoAccount)]
    public void SegmentOf_GroupsPassengersByHowTheyNeedToBeTold(PassengerCategory category, bool hasAccount, string expected)
    {
        var passenger = new Passenger { Category = category, UserId = hasAccount ? Guid.NewGuid() : null };

        Assert.Equal(expected, RecoveryMessageSegments.SegmentOf(passenger));
    }

    [Fact]
    public void DefaultMessage_AlwaysStatesRouteTimeAndBay()
    {
        var time = new DateTime(2026, 10, 20, 4, 15, 0, DateTimeKind.Utc);
        foreach (var segment in RecoveryMessageSegments.All)
        {
            var message = RecoveryMessageSegments.DefaultMessage(segment, "R-1", time, "B7");

            Assert.Contains("R-1", message);
            Assert.Contains("04:15", message);
            Assert.Contains("B7", message);
        }
    }
}
