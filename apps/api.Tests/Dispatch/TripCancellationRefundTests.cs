using System.Net;
using System.Net.Http.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Dispatch;

/// <summary>Component C with D (DEF-14): cancelling a trip cancels its active bookings and returns the passengers' money.</summary>
public sealed class TripCancellationRefundTests
{
    private const decimal Fare = 100m;

    private sealed record Arranged(TestApiFactory Factory, FakePaymentGateway Gateway, TestWorld World, Trip Trip, HttpClient Dispatcher) : IDisposable
    {
        public void Dispose() { Dispatcher.Dispose(); Factory.Dispose(); }
        public Task<decimal> BalanceAsync(Guid passengerId) =>
            Factory.WithDbAsync(db => db.Wallets.Where(w => w.PassengerId == passengerId).Select(w => w.Balance).SingleAsync());
    }

    private static async Task<Arranged> ArrangeAsync()
    {
        var gateway = new FakePaymentGateway();
        var factory = new TestApiFactory(gateway.Register);
        await factory.InitializeDatabaseAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        return new Arranged(factory, gateway, world, trip, factory.CreateClientAs("Dispatcher", centreId: world.Centre.Id));
    }

    /// <summary>A passenger with 500 in the wallet who books the trip through the API (wallet debit of 100).</summary>
    private static async Task<(Guid PassengerId, Guid BookingId)> BookByWalletAsync(Arranged a)
    {
        var (passenger, _) = await TestWorld.AddPassengerAsync(a.Factory, a.World, 500m, Fare);
        var response = await a.Dispatcher.PostAsJsonAsync("/api/v1/bookings", new { tripId = a.Trip.Id, passengerId = passenger.Id, passengerCount = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(400m, await a.BalanceAsync(passenger.Id));
        var bookingId = await a.Factory.WithDbAsync(db => db.Bookings.Where(b => b.PassengerId == passenger.Id).Select(b => b.Id).SingleAsync());
        return (passenger.Id, bookingId);
    }

    private static Task<HttpResponseMessage> CancelTrip(Arranged a, string reason = "Road closed") =>
        a.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{a.Trip.Id}") { Content = JsonContent.Create(new { reason }) });

    [Fact]
    public async Task Delete_RefundsEveryActiveBooking_WithOneRefundTransactionEach()
    {
        using var a = await ArrangeAsync();
        var first = await BookByWalletAsync(a);
        var second = await BookByWalletAsync(a);

        var response = await CancelTrip(a);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(500m, await a.BalanceAsync(first.PassengerId));
        Assert.Equal(500m, await a.BalanceAsync(second.PassengerId));
        var bookings = await a.Factory.WithDbAsync(db => db.Bookings.AsNoTracking().ToListAsync());
        Assert.All(bookings, b =>
        {
            Assert.Equal(BookingStatus.Cancelled, b.Status);
            Assert.Equal(Fare, b.RefundAmount);
            Assert.Equal("Trip cancelled: Road closed", b.CancellationReason);
        });
        Assert.Equal(2, await a.Factory.WithDbAsync(db => db.Transactions.CountAsync(t => t.Type == TransactionType.Refund)));
        Assert.Equal(TripStatus.Cancelled, (await a.Factory.WithDbAsync(db => db.Trips.FindAsync(a.Trip.Id).AsTask()))!.Status);
    }

    [Fact]
    public async Task PatchStatusToCancelled_DoesTheSameAsDelete()
    {
        using var a = await ArrangeAsync();
        var booked = await BookByWalletAsync(a);

        var response = await a.Dispatcher.PatchAsJsonAsync($"/api/v1/trips/{a.Trip.Id}/status", new { status = "Cancelled", note = "Driver unavailable" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(500m, await a.BalanceAsync(booked.PassengerId));
        var trip = await a.Factory.WithDbAsync(db => db.Trips.AsNoTracking().SingleAsync());
        Assert.Equal(TripStatus.Cancelled, trip.Status);
        Assert.Equal("Driver unavailable", trip.CancellationReason);
        var booking = await a.Factory.WithDbAsync(db => db.Bookings.AsNoTracking().SingleAsync());
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Contains("Driver unavailable", booking.CancellationReason);
    }

    [Fact]
    public async Task Delete_LeavesCompletedAndAlreadyCancelledBookingsAlone()
    {
        using var a = await ArrangeAsync();
        var active = await BookByWalletAsync(a);
        var alreadyCancelled = await BookByWalletAsync(a);
        var cancelledEarlier = await a.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{alreadyCancelled.BookingId}")
        {
            Content = JsonContent.Create(new { reason = "Passenger changed plans" })
        });
        Assert.Equal(HttpStatusCode.NoContent, cancelledEarlier.StatusCode);
        var (completedPassenger, _) = await TestWorld.AddPassengerAsync(a.Factory, a.World, 400m);
        await a.Factory.WithDbAsync(async db =>
        {
            db.Add(new Booking { TripId = a.Trip.Id, PassengerId = completedPassenger.Id, SeatNumber = "C1", QrCode = "BKG-C", Fare = Fare, Status = BookingStatus.Completed });
            await db.SaveChangesAsync();
        });

        var response = await CancelTrip(a);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(500m, await a.BalanceAsync(active.PassengerId)); // refunded now
        Assert.Equal(500m, await a.BalanceAsync(alreadyCancelled.PassengerId)); // refunded once, earlier, not again
        Assert.Equal(400m, await a.BalanceAsync(completedPassenger.Id)); // a completed ride is not refunded
        Assert.Equal(BookingStatus.Completed, (await a.Factory.WithDbAsync(db => db.Bookings.SingleAsync(b => b.SeatNumber == "C1"))).Status);
        Assert.Equal(1, await a.Factory.WithDbAsync(db => db.Transactions.CountAsync(t => t.BookingId == alreadyCancelled.BookingId && t.Type == TransactionType.Refund)));
    }

    [Fact]
    public async Task Delete_RefundsCardBookingsThroughStripe_AndWalletBookingsThroughTheWallet()
    {
        using var a = await ArrangeAsync();
        var wallet = await BookByWalletAsync(a);
        var (cardPassenger, _) = await TestWorld.AddPassengerAsync(a.Factory, a.World, 0m);
        var cardBookingId = await a.Factory.WithDbAsync(async db =>
        {
            var booking = new Booking { TripId = a.Trip.Id, PassengerId = cardPassenger.Id, SeatNumber = "S2", QrCode = "BKG-CARD", Fare = Fare, Status = BookingStatus.Confirmed };
            booking.Payments.Add(new Payment { Provider = PaymentProvider.Stripe, ProviderOrderId = "UPTS-CARD", ProviderCheckoutId = "cs_card", ProviderPaymentId = "pi_card", Amount = Fare, Status = PaymentStatus.Succeeded });
            db.Add(booking);
            await db.SaveChangesAsync();
            return booking.Id;
        });

        var response = await CancelTrip(a);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, a.Gateway.RefundCalls); // only the card booking goes to Stripe
        Assert.Equal(500m, await a.BalanceAsync(wallet.PassengerId));
        Assert.Equal(0m, await a.BalanceAsync(cardPassenger.Id)); // card money does not go to the wallet
        var card = await a.Factory.WithDbAsync(db => db.Bookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == cardBookingId));
        Assert.Equal(BookingStatus.Cancelled, card.Status);
        Assert.Equal(PaymentStatus.RefundPending, card.Payments.Single().Status);
        Assert.Equal(1, await a.Factory.CountAsync<PaymentRefund>());
    }

    [Fact]
    public async Task Delete_WhenAStripeRefundFails_StillCancelsTheTripAndRefundsWalletBookings_AndKeepsTheCardBookingForRetry()
    {
        using var a = await ArrangeAsync();
        var wallet = await BookByWalletAsync(a);
        var (cardPassenger, _) = await TestWorld.AddPassengerAsync(a.Factory, a.World, 0m);
        var cardBookingId = await a.Factory.WithDbAsync(async db =>
        {
            var booking = new Booking { TripId = a.Trip.Id, PassengerId = cardPassenger.Id, SeatNumber = "S2", QrCode = "BKG-CARD", Fare = Fare, Status = BookingStatus.Confirmed };
            booking.Payments.Add(new Payment { Provider = PaymentProvider.Stripe, ProviderOrderId = "UPTS-CARD", ProviderCheckoutId = "cs_card", ProviderPaymentId = "pi_card", Amount = Fare, Status = PaymentStatus.Succeeded });
            db.Add(booking);
            await db.SaveChangesAsync();
            return booking.Id;
        });
        a.Gateway.RefundResult = new(false, null, "stripe_unavailable");

        var response = await CancelTrip(a);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(TripStatus.Cancelled, (await a.Factory.WithDbAsync(db => db.Trips.FindAsync(a.Trip.Id).AsTask()))!.Status);
        Assert.Equal(500m, await a.BalanceAsync(wallet.PassengerId));
        var card = await a.Factory.WithDbAsync(db => db.Bookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == cardBookingId));
        Assert.Equal(BookingStatus.Confirmed, card.Status); // still refundable by staff through POST /payments/bookings/{id}/refund
        Assert.Equal(PaymentStatus.Succeeded, card.Payments.Single().Status);
        a.Gateway.RefundResult = new(true, "re_retry", null);
        var retry = await a.Dispatcher.PostAsJsonAsync($"/api/v1/payments/bookings/{cardBookingId}/refund", new { reason = "Retry after trip cancellation" });
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
    }

    [Fact]
    public async Task Delete_RollsEverythingBack_WhenABookingCannotBeRefunded()
    {
        using var a = await ArrangeAsync();
        var wallet = await BookByWalletAsync(a);
        var noWallet = await a.Factory.WithDbAsync(async db =>
        {
            var passenger = new Passenger { FullName = "No Wallet", PhoneNumber = "0700000000" };
            db.Add(passenger);
            db.Add(new Booking { TripId = a.Trip.Id, Passenger = passenger, SeatNumber = "S9", QrCode = "BKG-NW", Fare = Fare, Status = BookingStatus.Confirmed });
            await db.SaveChangesAsync();
            return passenger.Id;
        });

        var response = await CancelTrip(a);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Contains("could not be refunded", await response.Content.ReadAsStringAsync());
        Assert.Equal(TripStatus.Scheduled, (await a.Factory.WithDbAsync(db => db.Trips.FindAsync(a.Trip.Id).AsTask()))!.Status); // the trip is not cancelled with money still owed
        Assert.Equal(400m, await a.BalanceAsync(wallet.PassengerId)); // and nobody was half-refunded
        Assert.Equal(0, await a.Factory.WithDbAsync(db => db.Transactions.CountAsync(t => t.Type == TransactionType.Refund)));
        Assert.NotEqual(Guid.Empty, noWallet);
    }

    [Fact]
    public async Task Delete_OfATripThatIsAlreadyCancelled_IsRejected_AndRefundsNothingTwice()
    {
        using var a = await ArrangeAsync();
        var booked = await BookByWalletAsync(a);
        await CancelTrip(a);

        var second = await CancelTrip(a);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(500m, await a.BalanceAsync(booked.PassengerId));
    }
}
