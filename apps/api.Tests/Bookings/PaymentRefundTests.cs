using System.Net;
using System.Net.Http.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: refunding a card payment, both through /payments and through booking cancellation.</summary>
public sealed class PaymentRefundTests
{
    private sealed record Arranged(TestApiFactory Factory, FakePaymentGateway Gateway, Guid BookingId, HttpClient Client) : IDisposable
    {
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }

    private static async Task<Arranged> ArrangeAsync(PaymentStatus paymentStatus = PaymentStatus.Succeeded, BookingStatus bookingStatus = BookingStatus.Confirmed, bool withPayment = true)
    {
        var gateway = new FakePaymentGateway();
        var factory = new TestApiFactory(gateway.Register);
        await factory.InitializeDatabaseAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        var bookingId = await factory.WithDbAsync(async db =>
        {
            var booking = new Booking { TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = "S1", QrCode = "BKG-R", Fare = 100m, Status = bookingStatus };
            if (withPayment)
                booking.Payments.Add(new Payment { Provider = PaymentProvider.Stripe, ProviderOrderId = "UPTS-R", ProviderCheckoutId = "cs_r", ProviderPaymentId = "pi_r", Amount = 100m, Status = paymentStatus });
            db.Add(booking);
            await db.SaveChangesAsync();
            return booking.Id;
        });
        return new Arranged(factory, gateway, bookingId, factory.CreateClientAs("CentreManager", centreId: world.Centre.Id));
    }

    private static Task<HttpResponseMessage> Refund(Arranged a, string reason = "Passenger request") =>
        a.Client.PostAsJsonAsync($"/api/v1/payments/bookings/{a.BookingId}/refund", new { reason });

    [Fact]
    public async Task Refund_OfAPaidBooking_AsksStripe_CancelsTheBooking_AndMarksTheRefundPending()
    {
        using var a = await ArrangeAsync();

        var response = await Refund(a, "  Bus cancelled ");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, a.Gateway.RefundCalls);
        var booking = await a.Factory.WithDbAsync(db => db.Bookings.AsNoTracking().SingleAsync());
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(100m, booking.RefundAmount);
        Assert.Equal("Bus cancelled", booking.CancellationReason);
        Assert.Equal(PaymentStatus.RefundPending, (await a.Factory.WithDbAsync(db => db.Payments.SingleAsync())).Status);
        var refund = await a.Factory.WithDbAsync(db => db.PaymentRefunds.SingleAsync());
        Assert.Equal(PaymentRefundStatus.Requested, refund.Status);
        Assert.Equal("re_test", refund.ProviderRefundId);
    }

    [Fact]
    public async Task Refund_RejectedByStripe_Returns502_RecordsTheFailure_AndKeepsTheBooking()
    {
        using var a = await ArrangeAsync();
        a.Gateway.RefundResult = new(false, null, "charge_already_refunded");

        var response = await Refund(a);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(BookingStatus.Confirmed, (await a.Factory.WithDbAsync(db => db.Bookings.SingleAsync())).Status);
        Assert.Equal(PaymentStatus.Succeeded, (await a.Factory.WithDbAsync(db => db.Payments.SingleAsync())).Status);
        var refund = await a.Factory.WithDbAsync(db => db.PaymentRefunds.SingleAsync());
        Assert.Equal(PaymentRefundStatus.Failed, refund.Status);
        Assert.Equal("charge_already_refunded", refund.FailureReason);
    }

    [Fact]
    public async Task Refund_ForABookingWithoutASuccessfulPayment_IsRejected_WithoutCallingStripe()
    {
        using var unpaid = await ArrangeAsync(PaymentStatus.Pending, BookingStatus.Pending);
        using var none = await ArrangeAsync(withPayment: false);

        Assert.Equal(HttpStatusCode.BadRequest, (await Refund(unpaid)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Refund(none)).StatusCode);
        Assert.Equal(0, unpaid.Gateway.RefundCalls + none.Gateway.RefundCalls);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task Refund_ForAFinishedBooking_IsRejected(BookingStatus status)
    {
        using var a = await ArrangeAsync(bookingStatus: status);

        Assert.Equal(HttpStatusCode.BadRequest, (await Refund(a)).StatusCode);
        Assert.Equal(0, a.Gateway.RefundCalls);
    }

    [Fact]
    public async Task Refund_ForUnknownBooking_OrWithoutAReason_IsRejected()
    {
        using var a = await ArrangeAsync();

        var unknown = await a.Client.PostAsJsonAsync($"/api/v1/payments/bookings/{Guid.NewGuid()}/refund", new { reason = "x" });
        var blank = await Refund(a, "");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
    }

    [Fact]
    public async Task Refund_Twice_OnlyReachesStripeOnce()
    {
        using var a = await ArrangeAsync();

        var first = await Refund(a);
        var second = await Refund(a);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal(1, a.Gateway.RefundCalls);
    }

    [Fact]
    public async Task CancellingAPaidBooking_ThroughTheBookingApi_RefundsToStripe_NotToTheWallet()
    {
        using var a = await ArrangeAsync();

        var response = await a.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{a.BookingId}")
        {
            Content = JsonContent.Create(new { reason = "Changed plans" })
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, a.Gateway.RefundCalls);
        Assert.Equal(0, await a.Factory.CountAsync<Transaction>()); // no wallet credit
        Assert.Equal(PaymentStatus.RefundPending, (await a.Factory.WithDbAsync(db => db.Payments.SingleAsync())).Status);
    }

    [Fact]
    public async Task CancellingABookingWhosePaymentNeverSucceeded_JustCancelsIt()
    {
        using var a = await ArrangeAsync(PaymentStatus.Pending, BookingStatus.Pending);

        var response = await a.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{a.BookingId}")
        {
            Content = JsonContent.Create(new { reason = "Not paying" })
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, a.Gateway.RefundCalls);
        Assert.Equal(BookingStatus.Cancelled, (await a.Factory.WithDbAsync(db => db.Bookings.SingleAsync())).Status);
    }
}
