using System.Net;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: Stripe webhooks, using real signature verification with a test secret.</summary>
public sealed class StripeWebhookTests
{
    private const string OrderId = "UPTS-TESTORDER";
    private const string CheckoutId = "cs_test_1";

    private sealed record Arranged(TestApiFactory Factory, Guid PaymentId, Guid BookingId) : IDisposable
    {
        public void Dispose() => Factory.Dispose();
    }

    private static async Task<Arranged> ArrangeAsync(PaymentStatus status = PaymentStatus.Pending, BookingStatus bookingStatus = BookingStatus.Pending)
    {
        var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 0);
        var ids = await factory.WithDbAsync(async db =>
        {
            var booking = new Booking { TripId = trip.Id, PassengerId = passenger.Id, SeatNumber = "S1", QrCode = "BKG-W", Fare = 100m, Status = bookingStatus };
            var payment = new Payment
            {
                Booking = booking, Provider = PaymentProvider.Stripe, ProviderOrderId = OrderId, ProviderCheckoutId = CheckoutId,
                ProviderPaymentId = "pi_test_1", Amount = 100m, Currency = "LKR", Status = status
            };
            db.Add(payment);
            await db.SaveChangesAsync();
            return (payment.Id, booking.Id);
        });
        return new Arranged(factory, ids.Item1, ids.Item2);
    }

    private static async Task<(Payment Payment, Booking Booking)> LoadAsync(Arranged a) =>
        await a.Factory.WithDbAsync(async db => (await db.Payments.AsNoTracking().SingleAsync(), await db.Bookings.AsNoTracking().SingleAsync()));

    [Fact]
    public async Task CheckoutCompleted_ConfirmsTheBookingAndMarksThePaymentSucceeded()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        var response = await client.SendAsync(StripeWebhook.Create("evt_1", "checkout.session.completed",
            StripeWebhook.CheckoutSession(CheckoutId, "pi_new", OrderId, 10000)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (payment, booking) = await LoadAsync(a);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("pi_new", payment.ProviderPaymentId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(1, await a.Factory.CountAsync<PaymentWebhookEvent>());
    }

    [Fact]
    public async Task ReplayedEvent_IsIgnored_AndDoesNotRecordTwice()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();
        var session = StripeWebhook.CheckoutSession(CheckoutId, "pi_new", OrderId, 10000);

        await client.SendAsync(StripeWebhook.Create("evt_same", "checkout.session.completed", session));
        var again = await client.SendAsync(StripeWebhook.Create("evt_same", "checkout.session.completed", session));

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(1, await a.Factory.CountAsync<PaymentWebhookEvent>());
    }

    [Theory]
    [InlineData(9999, "lkr")]
    [InlineData(10000, "usd")]
    [InlineData(1, "lkr")]
    public async Task CheckoutCompleted_WithTheWrongAmountOrCurrency_DoesNotConfirmTheBooking(long amount, string currency)
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        var response = await client.SendAsync(StripeWebhook.Create("evt_bad", "checkout.session.completed",
            StripeWebhook.CheckoutSession(CheckoutId, "pi_x", OrderId, amount, currency)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (payment, booking) = await LoadAsync(a);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Fact]
    public async Task CheckoutCompleted_ForUnknownOrder_IsAcknowledgedAndIgnored()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        var response = await client.SendAsync(StripeWebhook.Create("evt_u", "checkout.session.completed",
            StripeWebhook.CheckoutSession("cs_other", "pi_x", "UPTS-OTHER", 10000)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Pending, (await LoadAsync(a)).Payment.Status);
    }

    [Fact]
    public async Task InvalidSignature_IsRejected_AndNothingChanges()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        var response = await client.SendAsync(StripeWebhook.Create("evt_f", "checkout.session.completed",
            StripeWebhook.CheckoutSession(CheckoutId, "pi_x", OrderId, 10000), secret: "whsec_wrong"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PaymentStatus.Pending, (await LoadAsync(a)).Payment.Status);
    }

    [Fact]
    public async Task MissingSignatureHeader_OrStaleTimestamp_IsRejected()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();
        var session = StripeWebhook.CheckoutSession(CheckoutId, "pi_x", OrderId, 10000);
        var unsigned = StripeWebhook.Create("evt_n", "checkout.session.completed", session);
        unsigned.Headers.Remove("Stripe-Signature");

        var stale = await client.SendAsync(StripeWebhook.Create("evt_s", "checkout.session.completed", session, signedAt: DateTimeOffset.UtcNow.AddHours(-2)));
        var none = await client.SendAsync(unsigned);

        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, none.StatusCode);
        Assert.Equal(PaymentStatus.Pending, (await LoadAsync(a)).Payment.Status);
    }

    [Fact]
    public async Task CheckoutExpired_CancelsThePaymentAndReleasesTheSeat()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        await client.SendAsync(StripeWebhook.Create("evt_e", "checkout.session.expired", $$"""{"id":"{{CheckoutId}}","object":"checkout.session"}"""));

        var (payment, booking) = await LoadAsync(a);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Contains("expired", booking.CancellationReason);
    }

    [Fact]
    public async Task PaymentFailed_CancelsTheBooking()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        await client.SendAsync(StripeWebhook.Create("evt_pf", "payment_intent.payment_failed", """{"id":"pi_test_1","object":"payment_intent"}"""));

        var (payment, booking) = await LoadAsync(a);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task ExpiredOrFailedEvents_DoNotUndoAPaymentThatAlreadySucceeded()
    {
        using var a = await ArrangeAsync(PaymentStatus.Succeeded, BookingStatus.Confirmed);
        using var client = a.Factory.CreateAnonymousClient();

        await client.SendAsync(StripeWebhook.Create("evt_e2", "checkout.session.expired", $$"""{"id":"{{CheckoutId}}","object":"checkout.session"}"""));
        await client.SendAsync(StripeWebhook.Create("evt_f2", "payment_intent.payment_failed", """{"id":"pi_test_1","object":"payment_intent"}"""));

        var (payment, booking) = await LoadAsync(a);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Theory]
    [InlineData("succeeded", PaymentStatus.Refunded, PaymentRefundStatus.Succeeded)]
    [InlineData("failed", PaymentStatus.Succeeded, PaymentRefundStatus.Failed)]
    public async Task RefundUpdated_UpdatesTheRefundAndPayment(string stripeStatus, PaymentStatus expectedPayment, PaymentRefundStatus expectedRefund)
    {
        using var a = await ArrangeAsync(PaymentStatus.RefundPending, BookingStatus.Cancelled);
        await a.Factory.WithDbAsync(async db =>
        {
            db.Add(new PaymentRefund { PaymentId = a.PaymentId, Amount = 100m, Reason = "test", ProviderRefundId = "re_1", Status = PaymentRefundStatus.Requested });
            await db.SaveChangesAsync();
        });
        using var client = a.Factory.CreateAnonymousClient();

        await client.SendAsync(StripeWebhook.Create("evt_r", "refund.updated", $$"""{"id":"re_1","object":"refund","status":"{{stripeStatus}}"}"""));

        Assert.Equal(expectedPayment, (await LoadAsync(a)).Payment.Status);
        Assert.Equal(expectedRefund, (await a.Factory.WithDbAsync(db => db.PaymentRefunds.SingleAsync())).Status);
    }

    [Fact]
    public async Task RefundUpdated_WithAnUnknownRefundOrPendingStatus_ChangesNothing()
    {
        using var a = await ArrangeAsync(PaymentStatus.RefundPending, BookingStatus.Cancelled);
        await a.Factory.WithDbAsync(async db =>
        {
            db.Add(new PaymentRefund { PaymentId = a.PaymentId, Amount = 100m, Reason = "test", ProviderRefundId = "re_1", Status = PaymentRefundStatus.Requested });
            await db.SaveChangesAsync();
        });
        using var client = a.Factory.CreateAnonymousClient();

        await client.SendAsync(StripeWebhook.Create("evt_r1", "refund.updated", """{"id":"re_unknown","object":"refund","status":"succeeded"}"""));
        await client.SendAsync(StripeWebhook.Create("evt_r2", "refund.updated", """{"id":"re_1","object":"refund","status":"pending"}"""));

        Assert.Equal(PaymentStatus.RefundPending, (await LoadAsync(a)).Payment.Status);
        Assert.Equal(PaymentRefundStatus.Requested, (await a.Factory.WithDbAsync(db => db.PaymentRefunds.SingleAsync())).Status);
    }

    [Fact]
    public async Task UnrelatedEventTypes_AreAcknowledged()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        var response = await client.SendAsync(StripeWebhook.Create("evt_o", "customer.created", """{"id":"cus_1","object":"customer"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
