using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.DTOs;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Notifications;

public sealed class MobileNotificationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private sealed record Arranged(
        TestApiFactory Factory,
        RecordingNotificationPublisher Publisher,
        TestWorld World,
        Trip Trip,
        Guid DriverUserId,
        HttpClient Dispatcher) : IDisposable
    {
        public void Dispose() { Dispatcher.Dispose(); Factory.Dispose(); }

        public Task<List<UserNotification>> NotificationsForAsync(Guid userId) =>
            Factory.WithDbAsync(db => db.UserNotifications.AsNoTracking().Where(n => n.UserId == userId).ToListAsync());
    }

    private static async Task<Arranged> ArrangeAsync()
    {
        var publisher = new RecordingNotificationPublisher();
        var factory = new TestApiFactory(publisher.Register);
        await factory.InitializeDatabaseAsync();
        var world = await TestWorld.SeedAsync(factory);
        var driverUserId = await LinkDriverAccountAsync(factory, world.Driver.Id);
        var trip = await TestWorld.AddTripAsync(factory, world);
        return new Arranged(factory, publisher, world, trip, driverUserId, factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id));
    }

    private static Task<Guid> LinkDriverAccountAsync(TestApiFactory factory, Guid driverId) =>
        factory.WithDbAsync(async db =>
        {
            var user = new User { UserName = $"d{Guid.NewGuid():N}@test.local", Email = $"d{Guid.NewGuid():N}@test.local", Name = "Driver", Role = UserRole.Driver };
            db.Add(user);
            var driver = await db.Drivers.SingleAsync(item => item.Id == driverId);
            driver.UserId = user.Id;
            await db.SaveChangesAsync();
            return user.Id;
        });

    private static async Task<Guid> BookAsync(Arranged a)
    {
        var (passenger, userId) = await TestWorld.AddPassengerAsync(a.Factory, a.World, 500m);
        var response = await a.Dispatcher.PostAsJsonAsync("/api/v1/bookings", new { tripId = a.Trip.Id, passengerId = passenger.Id, passengerCount = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return userId;
    }

    [Fact]
    public async Task Reassign_NotifiesPassengersOfNewBay_AndBothDrivers()
    {
        using var a = await ArrangeAsync();
        var passengerUserId = await BookAsync(a);
        var newBay = await TestWorld.AddBayAsync(a.Factory, a.World.Centre.Id, "B-NEW");
        var newDriver = await TestWorld.AddDriverAsync(a.Factory, a.World.Centre.Id, "LIC-NEW");
        var newDriverUserId = await LinkDriverAccountAsync(a.Factory, newDriver.Id);

        var response = await a.Dispatcher.PostAsJsonAsync($"/api/v1/trips/{a.Trip.Id}/reassign", new
        {
            vehicleId = a.World.Vehicle.Id,
            driverId = newDriver.Id,
            bayId = newBay.Id,
            scheduledTime = a.Trip.ScheduledTime
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var passengerNotification = Assert.Single(await a.NotificationsForAsync(passengerUserId));
        Assert.Equal(NotificationType.TripUpdated, passengerNotification.Type);
        Assert.Contains("B-NEW", passengerNotification.Message);
        Assert.Equal(NotificationType.TripUnassigned, Assert.Single(await a.NotificationsForAsync(a.DriverUserId)).Type);
        Assert.Equal(NotificationType.TripAssigned, Assert.Single(await a.NotificationsForAsync(newDriverUserId)).Type);
        Assert.Equal(3, a.Publisher.Sent.Count(item => item.Notification.TripId == a.Trip.Id));
    }

    [Fact]
    public async Task CancelTrip_NotifiesPassengerOfWalletRefund_AndDriver()
    {
        using var a = await ArrangeAsync();
        var passengerUserId = await BookAsync(a);

        var response = await a.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{a.Trip.Id}")
        {
            Content = JsonContent.Create(new { reason = "Road closed" })
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var passengerNotification = Assert.Single(await a.NotificationsForAsync(passengerUserId));
        Assert.Equal(NotificationType.BookingCancelled, passengerNotification.Type);
        Assert.Contains("refunded to your wallet", passengerNotification.Message);
        var driverNotification = Assert.Single(await a.NotificationsForAsync(a.DriverUserId));
        Assert.Equal(NotificationType.TripCancelled, driverNotification.Type);
        Assert.Contains("Road closed", driverNotification.Message);
    }

    [Fact]
    public async Task DriverStartsBoarding_NotifiesPassengersOnly()
    {
        using var a = await ArrangeAsync();
        var passengerUserId = await BookAsync(a);
        using var driver = a.Factory.CreateClientAs("Driver", a.DriverUserId);

        Assert.Equal(HttpStatusCode.NoContent, (await driver.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/status", new { status = "Ready" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await driver.PatchAsJsonAsync($"/api/v1/drivers/me/trips/{a.Trip.Id}/status", new { status = "Boarding" })).StatusCode);

        var notification = Assert.Single(await a.NotificationsForAsync(passengerUserId));
        Assert.Equal(NotificationType.TripStatusChanged, notification.Type);
        Assert.Contains(a.World.Bay.Code, notification.Message);
        Assert.Empty(await a.NotificationsForAsync(a.DriverUserId));
    }

    [Fact]
    public async Task FailedRequest_DoesNotPushNotifications()
    {
        using var a = await ArrangeAsync();
        await BookAsync(a);
        await a.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{a.Trip.Id}") { Content = JsonContent.Create(new { reason = "First" }) });
        a.Publisher.Sent.Clear();

        var response = await a.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{a.Trip.Id}") { Content = JsonContent.Create(new { reason = "Again" }) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(a.Publisher.Sent);
    }

    [Fact]
    public async Task Inbox_ListsOnlyOwnNotifications_AndMarksThemRead()
    {
        using var a = await ArrangeAsync();
        var passengerUserId = await BookAsync(a);
        await a.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{a.Trip.Id}") { Content = JsonContent.Create(new { reason = "Road closed" }) });
        using var commuter = a.Factory.CreateClientAs("Commuter", passengerUserId);
        using var otherCommuter = a.Factory.CreateClientAs("Commuter");

        var inbox = await commuter.GetFromJsonAsync<List<NotificationResponse>>("/api/v1/notifications", Json);
        var otherInbox = await otherCommuter.GetFromJsonAsync<List<NotificationResponse>>("/api/v1/notifications", Json);
        Assert.Single(inbox!);
        Assert.Empty(otherInbox!);

        Assert.Equal(HttpStatusCode.NotFound, (await otherCommuter.PostAsync($"/api/v1/notifications/{inbox![0].Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await commuter.PostAsync("/api/v1/notifications/read-all", null)).StatusCode);
        Assert.All(await a.NotificationsForAsync(passengerUserId), n => Assert.NotNull(n.ReadAt));
    }
}
