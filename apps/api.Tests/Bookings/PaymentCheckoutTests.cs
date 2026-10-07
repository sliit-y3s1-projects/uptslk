using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: starting a card payment (Stripe Checkout) for a booking. Stripe itself is replaced by a fake.</summary>
public sealed class PaymentCheckoutTests
{
    private sealed record Arranged(TestApiFactory Factory, FakePaymentGateway Gateway, TestWorld World, Trip Trip, Passenger Passenger, Guid UserId) : IDisposable
    {
        public void Dispose() => Factory.Dispose();
    }

    private static async Task<Arranged> ArrangeAsync(int capacity = 40, bool fareRule = true)
    {
        var gateway = new FakePaymentGateway();
        var factory = new TestApiFactory(gateway.Register);
        await factory.InitializeDatabaseAsync();
        var world = await TestWorld.SeedAsync(factory);
        if (capacity != 40)
            await factory.WithDbAsync(async db => { (await db.Vehicles.FindAsync(world.Vehicle.Id))!.Capacity = capacity; await db.SaveChangesAsync(); });
        var trip = await TestWorld.AddTripAsync(factory, world);
        var (passenger, userId) = await TestWorld.AddPassengerAsync(factory, world, 0, 100m, addFareRule: fareRule);
        return new Arranged(factory, gateway, world, trip, passenger, userId);
    }

    [Fact]
    public async Task CommuterCheckout_CreatesAPendingBookingAndPayment_AndReturnsTheCheckoutUrl()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id, passengerCount = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = body.GetProperty("orderId").GetString()!;
        Assert.StartsWith("https://checkout.test/", body.GetProperty("url").GetString());
        var payment = await a.Factory.WithDbAsync(db => db.Payments.Include(p => p.Booking).SingleAsync());
        Assert.Equal(orderId, payment.ProviderOrderId);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(200m, payment.Amount);
        Assert.Equal("LKR", payment.Currency);
        Assert.Equal(BookingStatus.Pending, payment.Booking.Status);
        Assert.Equal(2, payment.Booking.PassengerCount);
        Assert.Equal($"cs_{orderId}", payment.ProviderCheckoutId);
        Assert.Equal(1, a.Gateway.CheckoutCalls);
    }

    [Fact]
    public async Task Checkout_PassesPassengerDetailsAndTheMobileFlagToTheGateway()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);

        await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id, useMobileReturnUrl = true });

        var request = Assert.Single(a.Gateway.CheckoutRequests);
        Assert.True(request.UseMobileReturnUrl);
        Assert.Equal(100m, request.Amount);
        Assert.Equal("Test", request.FirstName);
        Assert.Equal("Passenger", request.LastName);
    }

    [Fact]
    public async Task StaffCheckout_ForAPassenger_Works()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateClientAs("CentreManager", centreId: a.World.Centre.Id);

        var response = await client.PostAsJsonAsync("/api/v1/payments/checkout", new { tripId = a.Trip.Id, passengerId = a.Passenger.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WhenTheGatewayFails_CancelsTheBookingAndReturns503()
    {
        using var a = await ArrangeAsync();
        a.Gateway.CheckoutFailure = new InvalidOperationException("Stripe is not configured.");
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var payment = await a.Factory.WithDbAsync(db => db.Payments.Include(p => p.Booking).SingleAsync());
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(BookingStatus.Cancelled, payment.Booking.Status);
        // the seat is released again
        var seats = await a.Factory.CreateClientAs("Commuter").GetFromJsonAsync<JsonElement>($"/api/v1/bookings/trips/{a.Trip.Id}/seats");
        Assert.Equal(0, seats.GetProperty("occupied").GetInt32());
    }

    [Fact]
    public async Task Checkout_HoldsTheSeatsWhilePaymentIsPending()
    {
        using var a = await ArrangeAsync(capacity: 2);
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);

        var first = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id, passengerCount = 2 });
        var second = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id, passengerCount = 1 });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, a.Gateway.CheckoutCalls);
    }

    [Fact]
    public async Task Checkout_WithoutAFareRule_OrForAClosedTrip_IsRejected()
    {
        using var noFare = await ArrangeAsync(fareRule: false);
        using var closed = await ArrangeAsync();
        var past = await TestWorld.AddTripAsync(closed.Factory, closed.World, DateTime.UtcNow.AddHours(-2));

        using var c1 = noFare.Factory.CreateClientAs("Commuter", noFare.UserId);
        using var c2 = closed.Factory.CreateClientAs("Commuter", closed.UserId);

        Assert.Equal(HttpStatusCode.BadRequest, (await c1.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = noFare.Trip.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c2.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = past.Id })).StatusCode);
        Assert.Equal(0, noFare.Gateway.CheckoutCalls + closed.Gateway.CheckoutCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task Checkout_WithPassengerCountOutsideOneToTen_IsRejected(int count)
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);

        var response = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id, passengerCount = count });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_ForACommuterWithoutAProfile_OrANonCommuter_IsRefused()
    {
        using var a = await ArrangeAsync();
        using var stranger = a.Factory.CreateClientAs("Commuter", Guid.NewGuid());
        using var admin = a.Factory.CreateClientAs("Admin");

        Assert.Equal(HttpStatusCode.BadRequest, (await stranger.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id })).StatusCode);
    }

    [Fact]
    public async Task OrderStatus_ReconcilesAPaidCheckoutWithStripe()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);
        var started = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id });
        var orderId = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();
        a.Gateway.CheckoutStatus = new(true, "pi_paid", 10000, "lkr");

        var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/payments/orders/{orderId}");

        Assert.Equal("Succeeded", status.GetProperty("status").GetString());
        Assert.Equal(BookingStatus.Confirmed, (await a.Factory.WithDbAsync(db => db.Bookings.SingleAsync())).Status);
    }

    [Fact]
    public async Task OrderStatus_WhenStripeSaysUnpaidOrAmountDiffers_StaysPending_AndUnknownOrderIsNotFound()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateClientAs("Commuter", a.UserId);
        var started = await client.PostAsJsonAsync("/api/v1/payments/checkout/me", new { tripId = a.Trip.Id });
        var orderId = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

        a.Gateway.CheckoutStatus = new(false, null, null, null);
        var unpaid = await client.GetFromJsonAsync<JsonElement>($"/api/v1/payments/orders/{orderId}");
        a.Gateway.CheckoutStatus = new(true, "pi_x", 9999, "lkr"); // wrong amount
        var mismatch = await client.GetFromJsonAsync<JsonElement>($"/api/v1/payments/orders/{orderId}");

        Assert.Equal("Pending", unpaid.GetProperty("status").GetString());
        Assert.Equal("Pending", mismatch.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/payments/orders/UPTS-UNKNOWN")).StatusCode);
    }

    [Fact]
    public async Task MobileReturnAndCancel_RedirectIntoTheApp()
    {
        using var a = await ArrangeAsync();
        using var client = a.Factory.CreateAnonymousClient();

        var back = await client.GetAsync("/api/v1/payments/mobile-return?orderId=UPTS-1");
        var cancel = await client.GetAsync("/api/v1/payments/mobile-cancel?orderId=UPTS-1");
        var missing = await client.GetAsync("/api/v1/payments/mobile-cancel?orderId=");

        Assert.Equal(HttpStatusCode.Redirect, back.StatusCode);
        Assert.StartsWith("uptslk://payment-return?orderId=UPTS-1", back.Headers.Location!.OriginalString);
        Assert.StartsWith("uptslk://payment-cancel?orderId=UPTS-1", cancel.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }
}
