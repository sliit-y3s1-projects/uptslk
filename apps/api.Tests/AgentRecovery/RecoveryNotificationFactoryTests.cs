using api.Enums;
using api.Models;
using api.Services.AgentRecovery;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryNotificationFactoryTests
{
    [Fact]
    public void Create_DeliversInAppNotificationForLinkedCommuter()
    {
        var userId = Guid.NewGuid();
        var (workflow, booking) = CreateRecords(userId);
        var scheduledTime = new DateTime(2026, 9, 30, 8, 45, 0, DateTimeKind.Utc);
        var createdAt = new DateTime(2026, 9, 29, 14, 0, 0, DateTimeKind.Utc);

        var notification = RecoveryNotificationFactory.Create(
            workflow,
            booking,
            scheduledTime,
            "KAD-B02",
            createdAt);

        Assert.Equal(PassengerNotificationChannel.InApp, notification.Channel);
        Assert.Equal(PassengerNotificationStatus.Delivered, notification.Status);
        Assert.Equal(createdAt, notification.DeliveredAt);
        Assert.Null(notification.DeliveryError);
        Assert.Contains("EX-01", notification.Subject);
        Assert.Contains("KAD-B02", notification.Message);
        Assert.Contains("2026-09-30 08:45 UTC", notification.Message);
        Assert.DoesNotContain(booking.Passenger.PhoneNumber, notification.Message);
        Assert.DoesNotContain(booking.Passenger.Email!, notification.Message);
    }

    [Fact]
    public void Create_RecordsSafeFailureWhenPassengerHasNoLinkedAccount()
    {
        var (workflow, booking) = CreateRecords(null);

        var notification = RecoveryNotificationFactory.Create(
            workflow,
            booking,
            DateTime.UtcNow.AddMinutes(15),
            "KAD-B02",
            DateTime.UtcNow);

        Assert.Equal(PassengerNotificationStatus.Failed, notification.Status);
        Assert.Null(notification.DeliveredAt);
        Assert.Equal("Passenger has no linked commuter account.", notification.DeliveryError);
    }

    [Fact]
    public void Create_RejectsMissingBayCode()
    {
        var (workflow, booking) = CreateRecords(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => RecoveryNotificationFactory.Create(
            workflow,
            booking,
            DateTime.UtcNow,
            " ",
            DateTime.UtcNow));
    }

    private static (AgentWorkflow Workflow, Booking Booking) CreateRecords(Guid? userId)
    {
        var passenger = new Passenger
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = "Test Passenger",
            PhoneNumber = "0770000000",
            Email = "passenger@example.test"
        };
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            PassengerId = passenger.Id,
            Passenger = passenger,
            SeatNumber = "A1",
            QrCode = "TEST-QR"
        };
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            Trip = new Trip
            {
                Route = new Route
                {
                    RouteNumber = "EX-01",
                    Name = "Example route",
                    Origin = "Origin",
                    Destination = "Destination"
                }
            }
        };
        return (workflow, booking);
    }
}
