using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Models;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: the commuter-facing /me endpoints used by the Flutter app.</summary>
public sealed class CommuterBookingTests
{
    [Fact]
    public async Task Commuter_CanBookForOwnProfile_AndSeeOnlyOwnTickets()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (_, userId) = await TestWorld.AddPassengerAsync(factory, world, 500);
        var (_, otherUserId) = await TestWorld.AddPassengerAsync(factory, world, 500);
        using var me = factory.CreateClientAs("Commuter", userId);
        using var other = factory.CreateClientAs("Commuter", otherUserId);

        var created = await me.PostAsJsonAsync("/api/v1/bookings/me", new { tripId = trip.Id, passengerCount = 1 });
        var mine = await me.GetFromJsonAsync<JsonElement>("/api/v1/bookings/me");
        var theirs = await other.GetFromJsonAsync<JsonElement>("/api/v1/bookings/me");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(0, theirs.GetArrayLength());
        Assert.Equal("Upcoming", mine[0].GetProperty("ticketGroup").GetString());
        Assert.True(mine[0].GetProperty("canBoard").GetBoolean());
    }

    [Fact]
    public async Task Commuter_WithoutPassengerProfile_CannotBook()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        using var client = factory.CreateClientAs("Commuter", Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/v1/bookings/me", new { tripId = trip.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotUseMyBookingEndpoints()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateAnonymousClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/bookings/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/v1/bookings/me", new { tripId = Guid.NewGuid() })).StatusCode);
    }

    [Theory]
    [InlineData("Driver")]
    [InlineData("Admin")]
    [InlineData("Dispatcher")]
    public async Task NonCommuterRoles_CannotUseMyBookingEndpoints(string role)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/bookings/me")).StatusCode);
    }

    [Fact]
    public async Task Ticket_HidesQrCode_WhenTripHasDeparted()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world, status: api.Enums.TripStatus.Completed);
        var (passenger, userId) = await TestWorld.AddPassengerAsync(factory, world, 500);
        await factory.WithDbAsync(async db =>
        {
            db.Add(new Booking { TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = "S1", QrCode = "BKG-SECRET", Status = api.Enums.BookingStatus.Confirmed });
            await db.SaveChangesAsync();
        });
        using var client = factory.CreateClientAs("Commuter", userId);

        var tickets = await client.GetFromJsonAsync<JsonElement>("/api/v1/bookings/me");

        Assert.Equal(JsonValueKind.Null, tickets[0].GetProperty("qrCode").ValueKind);
        Assert.False(tickets[0].GetProperty("canBoard").GetBoolean());
        Assert.Equal("Past", tickets[0].GetProperty("ticketGroup").GetString());
    }

    [Fact]
    public async Task FareQuote_ReturnsActiveRuleAmount_AndNotFoundWithoutRule()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0, fare: 175m);
        using var client = factory.CreateClientAs("CentreManager");

        var quote = await client.GetFromJsonAsync<JsonElement>($"/api/v1/fare-rules/quote?tripId={trip.Id}&passengerId={passenger.Id}");
        await factory.WithDbAsync(async db =>
        {
            db.FareRules.RemoveRange(db.FareRules);
            await db.SaveChangesAsync();
        });
        var missing = await client.GetAsync($"/api/v1/fare-rules/quote?tripId={trip.Id}&passengerId={passenger.Id}");

        Assert.Equal(175m, quote.GetProperty("fare").GetDecimal());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
