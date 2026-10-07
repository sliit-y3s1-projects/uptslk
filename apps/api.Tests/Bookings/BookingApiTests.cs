using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D (Passengers and Fares): booking creation, wallet debit/refund, capacity and cancellation.</summary>
public sealed class BookingApiTests
{
    private const string Url = "/api/v1/bookings";

    private sealed record Scenario(TestApiFactory Factory, TestWorld World, Trip Trip, Passenger Passenger, Guid UserId) : IDisposable
    {
        public void Dispose() => Factory.Dispose();
    }

    private static async Task<Scenario> ArrangeAsync(decimal balance = 500m, decimal fare = 100m, int capacity = 40)
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        if (capacity != 40)
            await factory.WithDbAsync(async db =>
            {
                (await db.Vehicles.FindAsync(world.Vehicle.Id))!.Capacity = capacity;
                await db.SaveChangesAsync();
            });
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, userId) = await TestWorld.AddPassengerAsync(factory, world, balance, fare);
        return new Scenario(factory, world, trip, passenger, userId);
    }

    private static Task<decimal> BalanceAsync(Scenario s) =>
        s.Factory.WithDbAsync(db => db.Wallets.Where(w => w.PassengerId == s.Passenger.Id).Select(w => w.Balance).SingleAsync());

    [Fact]
    public async Task Create_DebitsWallet_RecordsFareTransaction_AndConfirmsBooking()
    {
        using var s = await ArrangeAsync(balance: 500, fare: 100);
        using var client = s.Factory.CreateClientAs("CentreManager", centreId: s.World.Centre.Id);

        var response = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200m, body.GetProperty("fare").GetDecimal());
        Assert.Equal("Confirmed", body.GetProperty("status").GetString());
        Assert.StartsWith("BKG-", body.GetProperty("qrCode").GetString());
        Assert.Equal(300m, await BalanceAsync(s));
        var transaction = await s.Factory.WithDbAsync(db => db.Transactions.SingleAsync());
        Assert.Equal(TransactionType.Fare, transaction.Type);
        Assert.Equal(200m, transaction.Amount);
    }

    [Fact]
    public async Task Create_WithInsufficientBalance_IsRejected_AndWalletUnchanged()
    {
        using var s = await ArrangeAsync(balance: 50, fare: 100);
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(50m, await BalanceAsync(s));
        Assert.Equal(0, await s.Factory.CountAsync<Booking>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task Create_WithPassengerCountOutsideOneToTen_IsRejected(int count)
    {
        using var s = await ArrangeAsync();
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = count });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await s.Factory.CountAsync<Booking>());
    }

    [Fact]
    public async Task Create_ForInactivePassenger_IsRejected()
    {
        using var s = await ArrangeAsync();
        await s.Factory.WithDbAsync(async db =>
        {
            (await db.Passengers.FindAsync(s.Passenger.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        });
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForUnknownTrip_IsRejected()
    {
        using var s = await ArrangeAsync();
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = Guid.NewGuid(), passengerId = s.Passenger.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForDepartedTrip_IsRejected()
    {
        using var s = await ArrangeAsync();
        var past = await TestWorld.AddTripAsync(s.Factory, s.World, DateTime.UtcNow.AddHours(-3));
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = past.Id, passengerId = s.Passenger.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(TripStatus.Cancelled)]
    [InlineData(TripStatus.Completed)]
    [InlineData(TripStatus.Dispatched)]
    public async Task Create_ForTripThatIsNotOpenForBooking_IsRejected(TripStatus status)
    {
        using var s = await ArrangeAsync();
        var closed = await TestWorld.AddTripAsync(s.Factory, s.World, TestWorld.FutureDeparture.AddHours(8), status);
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = closed.Id, passengerId = s.Passenger.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutActiveFareRule_IsRejected()
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var _ = factory;
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 500, addFareRule: false);
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { tripId = trip.Id, passengerId = passenger.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenTripWouldExceedVehicleCapacity_ReturnsConflict()
    {
        using var s = await ArrangeAsync(balance: 1000, capacity: 3);
        using var client = s.Factory.CreateClientAs("CentreManager");

        var first = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 2 });
        var second = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 2 });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, await s.Factory.CountAsync<Booking>());
    }

    [Fact]
    public async Task Seats_ReportsOccupiedAndAvailableCapacity()
    {
        using var s = await ArrangeAsync(balance: 1000, capacity: 10);
        using var client = s.Factory.CreateClientAs("CentreManager");
        await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 4 });

        var seats = await client.GetFromJsonAsync<JsonElement>($"{Url}/trips/{s.Trip.Id}/seats");

        Assert.Equal(10, seats.GetProperty("capacity").GetInt32());
        Assert.Equal(4, seats.GetProperty("occupied").GetInt32());
        Assert.Equal(6, seats.GetProperty("available").GetInt32());
        Assert.False(seats.GetProperty("isFull").GetBoolean());
    }

    [Fact]
    public async Task Cancel_RefundsWallet_RecordsRefundTransaction_AndKeepsFinancialHistory()
    {
        using var s = await ArrangeAsync(balance: 500, fare: 100);
        using var client = s.Factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 2 });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{id}")
        {
            Content = JsonContent.Create(new { reason = "Change of plans" })
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(500m, await BalanceAsync(s));
        var booking = await s.Factory.WithDbAsync(db => db.Bookings.FindAsync(id).AsTask());
        Assert.Equal(BookingStatus.Cancelled, booking!.Status);
        Assert.Equal(200m, booking.RefundAmount);
        var types = await s.Factory.WithDbAsync(db => db.Transactions.Select(t => t.Type).ToListAsync());
        Assert.Contains(TransactionType.Fare, types);
        Assert.Contains(TransactionType.Refund, types);
    }

    [Fact]
    public async Task Cancel_Twice_IsRejected_AndRefundsOnlyOnce()
    {
        using var s = await ArrangeAsync(balance: 500, fare: 100);
        using var client = s.Factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        HttpRequestMessage Cancel() => new(HttpMethod.Delete, $"{Url}/{id}") { Content = JsonContent.Create(new { reason = "x" }) };

        var first = await client.SendAsync(Cancel());
        var second = await client.SendAsync(Cancel());

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(500m, await BalanceAsync(s));
    }

    [Fact]
    public async Task Cancel_WithoutReason_IsRejected()
    {
        using var s = await ArrangeAsync();
        using var client = s.Factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{id}")
        {
            Content = JsonContent.Create(new { reason = "" })
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_UnknownBooking_ReturnsNotFound()
    {
        using var s = await ArrangeAsync();
        using var client = s.Factory.CreateClientAs("CentreManager");

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new { reason = "x" })
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Manifest_ExcludesCancelledBookings_AndSumsPassengers()
    {
        using var s = await ArrangeAsync(balance: 1000);
        using var client = s.Factory.CreateClientAs("CentreManager");
        var keep = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 3 });
        var drop = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id, passengerCount = 2 });
        var dropId = (await drop.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{Url}/{dropId}") { Content = JsonContent.Create(new { reason = "x" }) });

        var manifest = await client.GetFromJsonAsync<JsonElement>($"{Url}/trips/{s.Trip.Id}/manifest");

        Assert.Equal(HttpStatusCode.Created, keep.StatusCode);
        Assert.Equal(1, manifest.GetProperty("bookings").GetArrayLength());
        Assert.Equal(3, manifest.GetProperty("passengerCount").GetInt32());
    }

    [Fact]
    public async Task Complete_IsOnlyAllowedForConfirmedBookingsOnDispatchedOrCompletedTrips()
    {
        using var s = await ArrangeAsync();
        using var client = s.Factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var tooEarly = await client.PatchAsJsonAsync($"{Url}/{id}/status", new { status = "Completed" });
        await s.Factory.WithDbAsync(async db =>
        {
            (await db.Trips.FindAsync(s.Trip.Id))!.Status = TripStatus.Dispatched;
            await db.SaveChangesAsync();
        });
        var afterDispatch = await client.PatchAsJsonAsync($"{Url}/{id}/status", new { status = "Completed" });

        Assert.Equal(HttpStatusCode.BadRequest, tooEarly.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, afterDispatch.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsBookingDetailWithTransactions()
    {
        using var s = await ArrangeAsync();
        using var client = s.Factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detail = await client.GetFromJsonAsync<JsonElement>($"{Url}/{id}");

        Assert.Equal(s.Passenger.Id, detail.GetProperty("passenger").GetProperty("id").GetGuid());
        Assert.Equal(1, detail.GetProperty("transactions").GetArrayLength());
    }

    [Fact]
    public async Task List_FiltersByPassengerAndStatus()
    {
        using var s = await ArrangeAsync(balance: 1000);
        using var client = s.Factory.CreateClientAs("CentreManager");
        await client.PostAsJsonAsync(Url, new { tripId = s.Trip.Id, passengerId = s.Passenger.Id });

        var mine = await client.GetFromJsonAsync<JsonElement>($"{Url}?passengerId={s.Passenger.Id}");
        var cancelled = await client.GetFromJsonAsync<JsonElement>($"{Url}?status=Cancelled");

        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(0, cancelled.GetArrayLength());
    }
}
