using api.Enums;
using api.Models;
using api.Services.AgentRecovery;
using Microsoft.EntityFrameworkCore;
using Xunit;

using api.Tests.Shared;
namespace api.Tests.AgentRecovery;

public sealed class RecoveryActionExecutorIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_AppliesApprovedRecoveryAndCreatesPassengerNotificationAtomically()
    {
        await using var database = await TestDb.CreateAsync();
        var scenario = RecoveryToolIntegrationTests.CreateTripScenario();
        var replacementVehicle = RecoveryToolIntegrationTests.CreateVehicle(scenario.Centre, "REPLACEMENT", 55);
        var replacementDriver = RecoveryToolIntegrationTests.CreateDriver(scenario.Centre, "Replacement Driver", "LIC-REPLACEMENT");
        var replacementBay = RecoveryToolIntegrationTests.CreateBay(scenario.Centre, "B02", BayStatus.Available);
        var incident = new Incident
        {
            CentreId = scenario.Centre.Id,
            Centre = scenario.Centre,
            ReportedByName = "Test operator",
            TripId = scenario.Trip.Id,
            Trip = scenario.Trip,
            Type = IncidentType.Breakdown,
            Severity = IncidentSeverity.Medium,
            Title = "Engine fault",
            Description = "Vehicle cannot continue.",
            Status = IncidentStatus.InProgress,
            SlaDueAt = DateTime.UtcNow.AddHours(1)
        };
        var workflow = new AgentWorkflow
        {
            CentreId = scenario.Centre.Id,
            Centre = scenario.Centre,
            IncidentId = incident.Id,
            Incident = incident,
            TripId = scenario.Trip.Id,
            Trip = scenario.Trip,
            Objective = "Restore service safely.",
            Status = WorkflowStatus.PausedForApproval
        };
        var approval = new ApprovalRequest
        {
            WorkflowId = workflow.Id,
            Workflow = workflow,
            Reason = "Validated recovery requires approval.",
            Decision = ApprovalDecision.Approved,
            DecidedAt = DateTime.UtcNow
        };
        workflow.ApprovalRequests.Add(approval);
        var booking = RecoveryToolIntegrationTests.CreateBooking(
            scenario.Trip,
            "A1",
            1,
            BookingStatus.Confirmed);
        var recoveredTime = scenario.Trip.ScheduledTime.AddMinutes(15);
        var proposal = new RecoveryProposal(
            replacementVehicle.Id,
            replacementDriver.Id,
            replacementBay.Id,
            recoveredTime,
            1,
            []);

        database.Context.AddRange(
            scenario.Centre,
            scenario.Route,
            scenario.Vehicle,
            scenario.Driver,
            scenario.Bay,
            scenario.Trip,
            replacementVehicle,
            replacementDriver,
            replacementBay,
            incident,
            workflow,
            approval,
            booking);
        await database.Context.SaveChangesAsync();

        var executor = new RecoveryActionExecutor(
            database.Context,
            new PassengerNotificationService(database.Context));
        await executor.ExecuteAsync(workflow, approval, proposal, CancellationToken.None);

        database.Context.ChangeTracker.Clear();
        var savedWorkflow = await database.Context.AgentWorkflows
            .Include(item => item.Trip)
            .Include(item => item.Incident)
            .Include(item => item.ApprovalRequests)
            .SingleAsync(item => item.Id == workflow.Id);
        var notification = await database.Context.PassengerNotifications
            .SingleAsync(item => item.WorkflowId == workflow.Id);

        Assert.Equal(WorkflowStatus.Completed, savedWorkflow.Status);
        Assert.NotNull(savedWorkflow.CompletedAt);
        Assert.Equal(replacementVehicle.Id, savedWorkflow.Trip.VehicleId);
        Assert.Equal(replacementDriver.Id, savedWorkflow.Trip.DriverId);
        Assert.Equal(replacementBay.Id, savedWorkflow.Trip.BayId);
        Assert.Equal(recoveredTime, savedWorkflow.Trip.ScheduledTime);
        Assert.Equal(TripStatus.Delayed, savedWorkflow.Trip.Status);
        Assert.Equal(IncidentStatus.Resolved, savedWorkflow.Incident.Status);
        Assert.NotNull(savedWorkflow.Incident.ResolvedAt);
        Assert.NotNull(savedWorkflow.ApprovalRequests.Single().AppliedAt);
        Assert.Equal(PassengerNotificationStatus.Failed, notification.Status);
        Assert.Equal("Passenger has no linked commuter account.", notification.DeliveryError);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsUnapprovedRecoveryWithoutChangingOperationalData()
    {
        await using var database = await TestDb.CreateAsync();
        var workflow = new AgentWorkflow { Status = WorkflowStatus.PausedForApproval };
        var approval = new ApprovalRequest { Decision = ApprovalDecision.Pending };
        var proposal = new RecoveryProposal(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            0,
            []);
        var executor = new RecoveryActionExecutor(
            database.Context,
            new PassengerNotificationService(database.Context));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(workflow, approval, proposal, CancellationToken.None));

        Assert.Contains("approved", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(WorkflowStatus.PausedForApproval, workflow.Status);
    }
}
