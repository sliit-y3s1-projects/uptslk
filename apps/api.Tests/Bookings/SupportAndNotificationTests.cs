using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: support requests handled by staff, and in-app notifications read by commuters.</summary>
public sealed class SupportAndNotificationTests
{
    private const string Support = "/api/v1/support-requests";
    private const string Notifications = "/api/v1/passenger-notifications";

    private static object NewRequest(Guid? passengerId = null, Guid? tripId = null) => new
    {
        passengerId, tripId, type = "Assistance", priority = "High", subject = " Wheelchair ramp ", description = " Needs a ramp at bay 2 ", assignedTo = " Nimal "
    };

    [Fact]
    public async Task Create_SavesTrimmedRequest_AsOpen()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        using var client = factory.CreateClientAs("Dispatcher");

        var response = await client.PostAsJsonAsync(Support, NewRequest(passenger.Id, trip.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var stored = await factory.WithDbAsync(db => db.SupportRequests.FindAsync(id).AsTask());
        Assert.Equal("Wheelchair ramp", stored!.Subject);
        Assert.Equal("Nimal", stored.AssignedTo);
        Assert.Equal(SupportRequestStatus.Open, stored.Status);
        Assert.Equal(SupportRequestPriority.High, stored.Priority);
    }

    [Fact]
    public async Task Create_ForUnknownPassengerOrTrip_IsRejected()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Dispatcher");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Support, NewRequest(passengerId: Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Support, NewRequest(tripId: Guid.NewGuid()))).StatusCode);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("Subject", "")]
    public async Task Create_WithMissingSubjectOrDescription_IsRejected(string subject, string description)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Dispatcher");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Support, new { subject, description })).StatusCode);
    }

    [Fact]
    public async Task Update_ToResolved_SetsResolvedAt_AndReopeningClearsIt()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");
        var id = (await (await client.PostAsJsonAsync(Support, NewRequest())).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        object Body(string status) => new { priority = "Low", status, subject = "S", description = "D", resolution = " Ramp provided " };

        var resolved = await client.PutAsJsonAsync($"{Support}/{id}", Body("Resolved"));
        var afterResolve = await factory.WithDbAsync(db => db.SupportRequests.FindAsync(id).AsTask());
        await client.PutAsJsonAsync($"{Support}/{id}", Body("InProgress"));
        var afterReopen = await factory.WithDbAsync(db => db.SupportRequests.FindAsync(id).AsTask());

        Assert.Equal(HttpStatusCode.NoContent, resolved.StatusCode);
        Assert.NotNull(afterResolve!.ResolvedAt);
        Assert.Equal("Ramp provided", afterResolve.Resolution);
        Assert.Null(afterReopen!.ResolvedAt);
    }

    [Fact]
    public async Task Update_AndGet_OfUnknownRequest_ReturnNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Dispatcher");

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"{Support}/{Guid.NewGuid()}",
            new { priority = "Low", status = "Open", subject = "S", description = "D" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Support}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task List_FiltersByTypeStatusPassengerAndTrip()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        using var client = factory.CreateClientAs("Dispatcher");
        await client.PostAsJsonAsync(Support, NewRequest(passenger.Id, trip.Id));
        await client.PostAsJsonAsync(Support, new { type = "Support", priority = "Low", subject = "General", description = "Question" });

        Assert.Equal(2, (await client.GetFromJsonAsync<JsonElement>(Support)).GetArrayLength());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Support}?type=Assistance")).GetArrayLength());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Support}?passengerId={passenger.Id}")).GetArrayLength());
        Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>($"{Support}?tripId={trip.Id}")).GetArrayLength());
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>($"{Support}?status=Closed")).GetArrayLength());
    }

    // ---- passenger notifications ----

    private static async Task<(Guid UserId, Guid NotificationId)> SeedNotificationAsync(
        TestApiFactory factory, TestWorld world, Trip trip, PassengerNotificationStatus status = PassengerNotificationStatus.Delivered)
    {
        var (passenger, userId) = await TestWorld.AddPassengerAsync(factory, world, 0);
        var notificationId = await factory.WithDbAsync(async db =>
        {
            var incident = new Incident { CentreId = world.Centre.Id, TripId = trip.Id, ReportedByName = "x", Title = "t", Description = "d", SlaDueAt = DateTime.UtcNow };
            var booking = new Booking { TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = "S1", QrCode = "BKG-N", Status = BookingStatus.Confirmed };
            db.AddRange(incident, booking);
            await db.SaveChangesAsync();
            var workflow = new AgentWorkflow { CentreId = world.Centre.Id, IncidentId = incident.Id, TripId = trip.Id, Objective = "o" };
            db.Add(workflow);
            await db.SaveChangesAsync();
            var notification = new PassengerNotification
            {
                WorkflowId = workflow.Id, BookingId = booking.Id, PassengerId = passenger.Id, Subject = "Trip delayed",
                Message = "Your bus is delayed by 20 minutes.", Status = status, DeliveredAt = DateTime.UtcNow
            };
            db.Add(notification);
            await db.SaveChangesAsync();
            return notification.Id;
        });
        return (userId, notificationId);
    }

    [Fact]
    public async Task Commuter_SeesOnlyTheirOwnDeliveredNotifications()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (userId, _) = await SeedNotificationAsync(factory, world, trip);
        using var me = factory.CreateClientAs("Commuter", userId);
        using var stranger = factory.CreateClientAs("Commuter", Guid.NewGuid());

        var mine = await me.GetFromJsonAsync<JsonElement>(Notifications);
        var theirs = await stranger.GetFromJsonAsync<JsonElement>(Notifications);

        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal("Trip delayed", mine[0].GetProperty("subject").GetString());
        Assert.Equal(0, theirs.GetArrayLength());
    }

    [Fact]
    public async Task FailedNotifications_AreNotShownToTheCommuter()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (userId, _) = await SeedNotificationAsync(factory, world, trip, PassengerNotificationStatus.Failed);
        using var me = factory.CreateClientAs("Commuter", userId);

        Assert.Equal(0, (await me.GetFromJsonAsync<JsonElement>(Notifications)).GetArrayLength());
    }

    [Fact]
    public async Task MarkRead_SetsReadAtOnce_AndOnlyForTheOwner()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (userId, id) = await SeedNotificationAsync(factory, world, trip);
        using var me = factory.CreateClientAs("Commuter", userId);
        using var stranger = factory.CreateClientAs("Commuter", Guid.NewGuid());

        var byStranger = await stranger.PostAsync($"{Notifications}/{id}/read", null);
        var first = await me.PostAsync($"{Notifications}/{id}/read", null);
        var readAt = (await factory.WithDbAsync(db => db.PassengerNotifications.FindAsync(id).AsTask()))!.ReadAt;
        await Task.Delay(20);
        await me.PostAsync($"{Notifications}/{id}/read", null);
        var readAtAgain = (await factory.WithDbAsync(db => db.PassengerNotifications.FindAsync(id).AsTask()))!.ReadAt;

        Assert.Equal(HttpStatusCode.NotFound, byStranger.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.NotNull(readAt);
        Assert.Equal(readAt, readAtAgain); // the first read time is kept
    }

    [Fact]
    public async Task Notifications_AreForCommutersOnly()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var admin = factory.CreateClientAs("Admin");
        using var anon = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync(Notifications)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(Notifications)).StatusCode);
    }
}
