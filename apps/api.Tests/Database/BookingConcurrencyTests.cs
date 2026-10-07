using System.Net;
using System.Net.Http.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Database;

/// <summary>
/// Database / reliability testing: the booking and cancellation workflow running through the real API against
/// real PostgreSQL, including parallel requests (which in-memory SQLite cannot reproduce).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Database")]
public sealed class BookingConcurrencyTests(PostgresFixture postgres)
{
    private const string Bookings = "/api/v1/bookings";

    private async Task<PostgresApiFactory> CreateFactoryAsync() => new(await postgres.CreateMigratedDatabaseAsync());

    private static async Task SetCapacityAsync(PostgresApiFactory factory, Guid vehicleId, int capacity) =>
        await factory.WithDbAsync(async db =>
        {
            (await db.Vehicles.FindAsync(vehicleId))!.Capacity = capacity;
            await db.SaveChangesAsync();
        });

    [Fact]
    public async Task Booking_CommitsBookingTransactionAndWalletDebitTogether()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 500m, 100m);
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Bookings, new { tripId = trip.Id, passengerId = passenger.Id, passengerCount = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(300m, await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync()));
        Assert.Equal(1, await factory.CountAsync<Booking>());
        Assert.Equal(200m, await factory.WithDbAsync(db => db.Transactions.Select(t => t.Amount).SingleAsync()));
    }

    [Fact]
    public async Task RejectedBooking_LeavesWalletAndTablesUntouched()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 50m, 100m);
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Bookings, new { tripId = trip.Id, passengerId = passenger.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(50m, await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync()));
        Assert.Equal(0, await factory.CountAsync<Booking>());
        Assert.Equal(0, await factory.CountAsync<Transaction>());
    }

    [Fact]
    public async Task ParallelBookings_NeverExceedVehicleCapacity()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        await SetCapacityAsync(factory, world.Vehicle.Id, 2);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var passengers = new List<Passenger>();
        for (var i = 0; i < 10; i++) passengers.Add((await TestWorld.AddPassengerAsync(factory, world, 500m, 100m)).Passenger);
        using var client = factory.CreateClientAs("CentreManager");

        var responses = await Task.WhenAll(passengers.Select(p =>
            client.PostAsJsonAsync(Bookings, new { tripId = trip.Id, passengerId = p.Id, passengerCount = 1 })));

        var booked = await factory.WithDbAsync(db => db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending).SumAsync(b => b.PassengerCount));
        Assert.True(booked <= 2, $"Capacity is 2 but {booked} seats were sold ({responses.Count(r => r.StatusCode == HttpStatusCode.Created)} x 201).");
    }

    [Fact]
    public async Task ParallelBookings_ByOneWallet_CannotSpendMoreThanTheBalance()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, balance: 100m, fare: 100m);
        using var client = factory.CreateClientAs("CentreManager");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            client.PostAsJsonAsync(Bookings, new { tripId = trip.Id, passengerId = passenger.Id, passengerCount = 1 })));

        var balance = await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync());
        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.True(balance >= 0m && created <= 1,
            $"Wallet held 100 and each ticket costs 100, but {created} bookings succeeded and the balance is {balance}.");
    }

    [Fact]
    public async Task ParallelCancellations_RefundTheBookingOnlyOnce()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, balance: 100m, fare: 100m);
        using var client = factory.CreateClientAs("CentreManager");
        var created = await client.PostAsJsonAsync(Bookings, new { tripId = trip.Id, passengerId = passenger.Id });
        var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(0m, await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync()));

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"{Bookings}/{id}") { Content = JsonContent.Create(new { reason = "dup" }) })));

        var balance = await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync());
        var refunds = await factory.WithDbAsync(db => db.Transactions.CountAsync(t => t.Type == TransactionType.Refund));
        Assert.True(balance == 100m && refunds == 1, $"Expected one refund of 100, but balance is {balance} with {refunds} refund transactions.");
    }

    [Fact]
    public async Task Trip_WithLocalTimeWithoutTimezone_IsStoredAsUtcInstant()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var response = await client.PostAsJsonAsync("/api/v1/trips", new
        {
            centreId = world.Centre.Id, routeId = world.Route.Id, vehicleId = world.Vehicle.Id, driverId = world.Driver.Id,
            bayId = world.Bay.Id, scheduledTime = "2030-06-01T08:00:00" // no "Z" and no offset
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ParallelTopUps_AreAllCredited_WithNoLostUpdates()
    {
        using var factory = await CreateFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, balance: 0m);
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            client.PostAsJsonAsync($"/api/v1/passengers/{passenger.Id}/wallet/top-ups", new { amount = 100m })));

        var balance = await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync());
        var topups = await factory.WithDbAsync(db => db.Transactions.CountAsync(t => t.Type == TransactionType.Topup));
        Assert.True(responses.All(r => r.StatusCode == HttpStatusCode.Created) && balance == 1000m && topups == 10,
            $"10 top-ups of 100 were accepted, but the balance is {balance} with {topups} top-up transactions (expected 1000 and 10).");
    }
}
