using System.Net;
using System.Net.Http.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Database;

/// <summary>
/// Reliability testing on real PostgreSQL for the card-payment path (the one the mobile app uses):
/// parallel checkouts must not oversell a trip and parallel refund requests must reach Stripe only once.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Database")]
public sealed class PaymentConcurrencyTests(PostgresFixture postgres)
{
    [Fact]
    public async Task ParallelCheckouts_NeverHoldMoreSeatsThanTheVehicleHas()
    {
        var gateway = new FakePaymentGateway();
        using var factory = new PostgresApiFactory(await postgres.CreateMigratedDatabaseAsync(), gateway.Register);
        var world = await TestWorld.SeedAsync(factory);
        await factory.WithDbAsync(async db => { (await db.Vehicles.FindAsync(world.Vehicle.Id))!.Capacity = 2; await db.SaveChangesAsync(); });
        var trip = await TestWorld.AddTripAsync(factory, world);
        var users = new List<Guid>();
        for (var i = 0; i < 10; i++) users.Add((await TestWorld.AddPassengerAsync(factory, world, 0, 100m)).UserId);

        var responses = await Task.WhenAll(users.Select(userId =>
        {
            var client = factory.CreateClientAs("Commuter", userId);
            return client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = trip.Id, passengerCount = 1 });
        }));

        var held = await factory.WithDbAsync(db => db.Bookings
            .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed).SumAsync(b => b.PassengerCount));
        Assert.True(held <= 2, $"Capacity is 2 but {held} seats are held ({responses.Count(r => r.StatusCode == HttpStatusCode.OK)} checkouts started).");
    }

    [Fact]
    public async Task ParallelRefundRequests_ReachStripeOnlyOnce()
    {
        var gateway = new FakePaymentGateway { RefundDelay = TimeSpan.FromMilliseconds(300) };
        using var factory = new PostgresApiFactory(await postgres.CreateMigratedDatabaseAsync(), gateway.Register);
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        var bookingId = await factory.WithDbAsync(async db =>
        {
            var booking = new Booking { TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = "S1", QrCode = "BKG-P", Fare = 100m, Status = BookingStatus.Confirmed };
            booking.Payments.Add(new Payment { Provider = PaymentProvider.Stripe, ProviderOrderId = "UPTS-P", ProviderCheckoutId = "cs_p", ProviderPaymentId = "pi_p", Amount = 100m, Status = PaymentStatus.Succeeded });
            db.Add(booking);
            await db.SaveChangesAsync();
            return booking.Id;
        });
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);

        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            client.PostAsJsonAsync($"/api/v1/payments/bookings/{bookingId}/refund", new { reason = "double click" })));

        var refunds = await factory.CountAsync<PaymentRefund>();
        Assert.True(gateway.RefundCalls == 1 && refunds == 1, $"Stripe was asked {gateway.RefundCalls} times and {refunds} refund rows exist; expected exactly one.");
    }

    [Fact]
    public async Task ParallelCancellations_OfAPaidBooking_ThroughTheBookingApi_ReachStripeOnlyOnce()
    {
        var gateway = new FakePaymentGateway { RefundDelay = TimeSpan.FromMilliseconds(300) };
        using var factory = new PostgresApiFactory(await postgres.CreateMigratedDatabaseAsync(), gateway.Register);
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        var bookingId = await factory.WithDbAsync(async db =>
        {
            var booking = new Booking { TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = "S1", QrCode = "BKG-C", Fare = 100m, Status = BookingStatus.Confirmed };
            booking.Payments.Add(new Payment { Provider = PaymentProvider.Stripe, ProviderOrderId = "UPTS-C", ProviderCheckoutId = "cs_c", ProviderPaymentId = "pi_c", Amount = 100m, Status = PaymentStatus.Succeeded });
            db.Add(booking);
            await db.SaveChangesAsync();
            return booking.Id;
        });
        using var client = factory.CreateClientAs("CentreManager", centreId: world.Centre.Id);

        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{bookingId}") { Content = JsonContent.Create(new { reason = "double click" }) })));

        Assert.Equal(1, gateway.RefundCalls);
        Assert.Equal(1, await factory.CountAsync<PaymentRefund>());
    }

    [Fact]
    public async Task ParallelTripAndBookingCancellations_RefundThePassengerOnlyOnce()
    {
        // DEF-14: cancelling the trip refunds its bookings. Cancelling the trip and the booking at the same moment (a
        // dispatcher and a manager, or a double click) must still return the fare exactly once.
        using var factory = new PostgresApiFactory(await postgres.CreateMigratedDatabaseAsync());
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 500m, 100m);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);
        var booked = await client.PostAsJsonAsync("/api/v1/bookings", new { tripId = trip.Id, passengerId = passenger.Id, passengerCount = 1 });
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var bookingId = await factory.WithDbAsync(db => db.Bookings.Select(b => b.Id).SingleAsync());

        var calls = Enumerable.Range(0, 4).Select(_ => client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{trip.Id}") { Content = JsonContent.Create(new { reason = "Flooding" }) }))
            .Concat(Enumerable.Range(0, 4).Select(_ => client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{bookingId}") { Content = JsonContent.Create(new { reason = "Duplicate" }) })));
        await Task.WhenAll(calls);

        var balance = await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync());
        var refunds = await factory.WithDbAsync(db => db.Transactions.CountAsync(t => t.Type == TransactionType.Refund));
        Assert.True(balance == 500m && refunds == 1, $"Expected the wallet back at 500 with one refund, but it is {balance} with {refunds} refund transactions.");
    }

    [Fact]
    public async Task BookingWhileTheTripIsBeingCancelled_NeverLeavesAPaidBookingOnACancelledTrip()
    {
        using var factory = new PostgresApiFactory(await postgres.CreateMigratedDatabaseAsync());
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var passengers = new List<Passenger>();
        for (var i = 0; i < 8; i++) passengers.Add((await TestWorld.AddPassengerAsync(factory, world, 500m, 100m)).Passenger);
        using var client = factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id);

        var bookings = passengers.Select(p => client.PostAsJsonAsync("/api/v1/bookings", new { tripId = trip.Id, passengerId = p.Id, passengerCount = 1 }));
        var cancel = client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{trip.Id}") { Content = JsonContent.Create(new { reason = "Flooding" }) });
        await Task.WhenAll(bookings.Cast<Task>().Append(cancel));

        // Every passenger either could not book, or was refunded: nobody is charged for a cancelled trip.
        var active = await factory.WithDbAsync(db => db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
        var total = await factory.WithDbAsync(db => db.Wallets.SumAsync(w => w.Balance));
        Assert.True(active == 0 && total == 8 * 500m, $"{active} active bookings remain on a cancelled trip and the wallets hold {total} instead of {8 * 500m}.");
    }
}
