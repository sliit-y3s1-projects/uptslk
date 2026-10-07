using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Data;
using api.Enums;
using api.Models;
using api.Services;
using api.Services.Payments;
using api.Tests.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests.Bookings;

public sealed class BookingEligibilityTests
{
    [Theory]
    [InlineData(TripStatus.Dispatched)]
    [InlineData(TripStatus.Boarding)]
    [InlineData(TripStatus.Delayed)]
    public void OngoingTickets_MoveToPastAtExpectedEndPlusBuffer(TripStatus status)
    {
        var departure = new DateTime(2026, 9, 24, 0, 30, 0, DateTimeKind.Utc);
        var trip = new Trip {
            ScheduledTime = departure, Status = status,
            Route = new Route { EstimatedDurationMin = 120 },
            RouteDirection = new RouteDirection { EstimatedDurationMin = 90 }
        };
        var booking = new Booking { Trip = trip, Status = BookingStatus.Confirmed };
        var cutoff = departure.AddMinutes(150);
        Assert.Equal(cutoff, BookingEligibility.ActiveUntil(trip));
        Assert.Equal("Upcoming", BookingEligibility.TicketGroup(booking, cutoff.AddSeconds(-1)));
        Assert.Equal("Past", BookingEligibility.TicketGroup(booking, cutoff));
        Assert.False(BookingEligibility.CanBoard(booking, cutoff));
        Assert.Equal(status, trip.Status);

        trip.ActualDepartureAt = departure.AddHours(1);
        Assert.Equal(cutoff.AddHours(1), BookingEligibility.ActiveUntil(trip));
        Assert.Equal("Upcoming", BookingEligibility.TicketGroup(booking, cutoff));
        Assert.Equal("Past", BookingEligibility.TicketGroup(booking, departure.AddDays(6)));
    }

    [Fact]
    public void ActiveWindow_FallsBackToRouteThenOneHourDuration()
    {
        var trip = new Trip { ScheduledTime = DateTime.UtcNow, Route = new Route { EstimatedDurationMin = 45 } };
        Assert.Equal(trip.ScheduledTime.AddMinutes(105), BookingEligibility.ActiveUntil(trip));
        trip.Route.EstimatedDurationMin = 0;
        Assert.Equal(trip.ScheduledTime.AddHours(2), BookingEligibility.ActiveUntil(trip));
    }

    [Theory]
    [InlineData(-1, TripStatus.Scheduled, false)]
    [InlineData(0, TripStatus.Boarding, false)]
    [InlineData(1, TripStatus.Scheduled, true)]
    [InlineData(1, TripStatus.Ready, true)]
    [InlineData(1, TripStatus.Boarding, true)]
    [InlineData(1, TripStatus.Cancelled, false)]
    [InlineData(1, TripStatus.Completed, false)]
    [InlineData(1, TripStatus.Dispatched, false)]
    public void DepartureCutoff_IsEnforced(int minutes, TripStatus status, bool expected)
    {
        var now = DateTime.UtcNow;
        Assert.Equal(expected, BookingEligibility.IsOpenForBooking(new Trip { ScheduledTime = now.AddMinutes(minutes), Status = status }, now));
    }

    [Fact]
    public async Task PastDeparture_IsHiddenFromSearch_AndRejectedByBothBookingMethods()
    {
        using var factory = new TestApiFactory();
        await factory.InitializeDatabaseAsync();
        var (userId, tripId, _) = await SeedAsync(factory, DateTime.UtcNow.AddHours(-8));
        using var client = CommuterClient(factory, userId);
        var search = await client.GetFromJsonAsync<JsonElement>("/api/v1/trips?bookableOnly=true");
        Assert.Equal(0, search.GetArrayLength());
        var operational = await client.GetFromJsonAsync<JsonElement>("/api/v1/trips");
        Assert.Equal(1, operational.GetArrayLength());
        foreach (var endpoint in new[] { "/api/v1/bookings/me", "/api/v1/payments/checkout/me" })
        {
            var response = await client.PostAsJsonAsync(endpoint, new { tripId, passengerCount = 1 });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Bookings have closed", await response.Content.ReadAsStringAsync());
        }
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Payments.ToListAsync());
    }

    [Fact]
    public async Task TicketResponse_HidesCancelledAndUnpaidQr_PreservesConfirmedToken()
    {
        using var factory = new TestApiFactory();
        await factory.InitializeDatabaseAsync();
        var (userId, tripId, bookingId) = await SeedAsync(factory, DateTime.UtcNow.AddHours(3));
        using var client = CommuterClient(factory, userId);
        var tickets = await client.GetFromJsonAsync<JsonElement>("/api/v1/bookings/me");
        Assert.True(tickets[0].GetProperty("canBoard").GetBoolean());
        Assert.Equal("BKG-TEST-BOARDING", tickets[0].GetProperty("qrCode").GetString());
        foreach (var status in new[] { BookingStatus.Pending, BookingStatus.Cancelled, BookingStatus.Completed })
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Bookings.FindAsync(bookingId))!.Status = status;
            await db.SaveChangesAsync();
            tickets = await client.GetFromJsonAsync<JsonElement>("/api/v1/bookings/me");
            Assert.False(tickets[0].GetProperty("canBoard").GetBoolean());
            Assert.Equal(JsonValueKind.Null, tickets[0].GetProperty("qrCode").ValueKind);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Bookings.FindAsync(bookingId))!.Status = BookingStatus.Confirmed;
            (await db.Trips.FindAsync(tripId))!.Status = TripStatus.Cancelled;
            await db.SaveChangesAsync();
        }
        tickets = await client.GetFromJsonAsync<JsonElement>("/api/v1/bookings/me");
        Assert.Equal("Cancelled", tickets[0].GetProperty("ticketGroup").GetString());
        Assert.False(tickets[0].GetProperty("canBoard").GetBoolean());
    }

    [Fact]
    public async Task LatePaymentNotification_DoesNotReactivateCancelledTicket()
    {
        using var factory = new TestApiFactory();
        await factory.InitializeDatabaseAsync();
        var (_, _, bookingId) = await SeedAsync(factory, DateTime.UtcNow.AddHours(3));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = (await db.Bookings.FindAsync(bookingId))!;
        booking.Status = BookingStatus.Cancelled;
        db.Payments.Add(new Payment { Booking = booking, Provider = PaymentProvider.Stripe, ProviderOrderId = "ORDER-TEST", ProviderCheckoutId = "CHECKOUT-TEST", Amount = 100, Currency = "LKR", Status = PaymentStatus.Pending });
        await db.SaveChangesAsync();
        await new BookingPaymentService(db, []).HandleStripeCheckoutCompletedAsync("EVENT-TEST", "CHECKOUT-TEST", "PI-TEST", "ORDER-TEST", 10000, "lkr", default);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    private static HttpClient CommuterClient(TestApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Commuter");
        return client;
    }

    private static async Task<(Guid UserId, Guid TripId, Guid BookingId)> SeedAsync(TestApiFactory factory, DateTime departure)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var centre = new Centre { Code = "TEST", Name = "Test centre", City = "Colombo", District = "Colombo" };
        var trip = new Trip {
            Centre = centre, ScheduledTime = departure,
            Route = new Route { Centre = centre, RouteNumber = "TEST-1", Name = "Test journey", Origin = "Colombo", Destination = "Kandy" },
            Bay = new Bay { Centre = centre, Code = "B01" },
            Vehicle = new Vehicle { Centre = centre, PlateNumber = "TEST-001", Model = "Bus", Capacity = 40 },
            Driver = new Driver { Centre = centre, FullName = "Test Driver", LicenseNumber = "TEST-LIC" }
        };
        var user = new User { UserName = "test@example.test", Email = "test@example.test", Name = "Test Commuter", Role = UserRole.Commuter };
        var booking = new Booking { Trip = trip, Passenger = new Passenger { User = user, FullName = user.Name, PhoneNumber = "0770000000" }, Status = BookingStatus.Confirmed, SeatNumber = "REF-1", QrCode = "BKG-TEST-BOARDING" };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return (user.Id, trip.Id, booking.Id);
    }
}
