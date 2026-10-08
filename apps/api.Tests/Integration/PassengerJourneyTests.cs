using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Tests.Auth;
using api.Tests.Shared;
using Xunit;

namespace api.Tests.Integration;

/// <summary>
/// Integrated end-to-end workflow across all four components, driven only through the real HTTP API with real sign-in
/// (JWT) on in-memory SQLite:
///   A Centres and Network  -> centre, bay, route with stops
///   B Fleet                -> vehicle
///   C Scheduling/Dispatch  -> driver, trip, trip lifecycle, cancellation
///   D Passengers and Fares -> fare rule, commuter, wallet, booking, ticket, refund
/// Everything is created through the API. The only direct seeding is the first Admin account (no endpoint creates an
/// Admin; in production Program.cs creates the bootstrap admin at startup).
/// </summary>
public sealed class PassengerJourneyTests
{
    private const string Password = "Passw0rd!";
    private const decimal FareAmount = 150m;
    private const decimal TopUpAmount = 500m;

    private sealed record Journey(
        RealAuthApiFactory Factory, HttpClient Admin, HttpClient Manager, HttpClient FleetOfficer, HttpClient Dispatcher,
        HttpClient Commuter, Guid CentreId, Guid RouteId, Guid TripId, Guid PassengerId, Guid BookingId, string QrCode) : IDisposable
    {
        public void Dispose()
        {
            foreach (var client in new[] { Admin, Manager, FleetOfficer, Dispatcher, Commuter }) client.Dispose();
            Factory.Dispose();
        }
    }

    // ---------------------------------------------------------------- helpers

    private static async Task<HttpClient> SignInAsync(RealAuthApiFactory factory, string email)
    {
        var client = AuthenticationTests.NewClient(factory);
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string url, object body, string what)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.True(response.StatusCode == HttpStatusCode.Created,
            $"{what}: expected 201 Created but got {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<HttpClient> CreateStaffAsync(HttpClient admin, RealAuthApiFactory factory, string role, Guid centreId)
    {
        var email = $"{role.ToLowerInvariant()}@journey.test";
        var response = await admin.PostAsJsonAsync("/api/v1/auth/create-user", new { name = $"{role} Journey", email, password = Password, role, centreId });
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"create {role}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await SignInAsync(factory, email);
    }

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {url}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<decimal> WalletBalanceAsync(HttpClient staff, Guid passengerId) =>
        (await GetAsync(staff, $"/api/v1/passengers/{passengerId}")).GetProperty("wallet").GetProperty("balance").GetDecimal();

    private static async Task<JsonElement> SingleTicketAsync(HttpClient commuter)
    {
        var tickets = await GetAsync(commuter, "/api/v1/bookings/me");
        Assert.Equal(1, tickets.GetArrayLength());
        return tickets[0];
    }

    private static async Task<JsonElement> TripAsync(HttpClient staff, Guid tripId) => await GetAsync(staff, $"/api/v1/trips/{tripId}");

    // ---------------------------------------------------------------- steps 1 to 5 (shared by every test)

    private static async Task<Journey> ArrangeAsync()
    {
        var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("admin@journey.test", Password, UserRole.Admin); // seeded: no endpoint creates the first Admin
        var admin = await SignInAsync(factory, "admin@journey.test");

        // ---- Step 1, Component A (Network): Admin creates an operating centre, a bay and a route with two stops
        var centreId = await CreateAsync(admin, "/api/v1/centres",
            new { code = "JRN", name = "Journey Terminal", city = "Colombo", district = "Colombo", status = "Operating" }, "A: centre");
        var bayId = await CreateAsync(admin, $"/api/v1/centres/{centreId}/bays", new { code = "B1" }, "A: bay");
        var routeId = await CreateAsync(admin, "/api/v1/routes", new
        {
            centreId, routeNumber = "J1", name = "Colombo - Kandy", origin = "Colombo", destination = "Kandy", serviceType = "Normal",
            distanceKm = 115, estimatedDurationMin = 180,
            stops = new[] { new { stopName = "Colombo Fort", latitude = 6.93, longitude = 79.85 }, new { stopName = "Kandy", latitude = 7.29, longitude = 80.63 } }
        }, "A: route");
        var route = await GetAsync(admin, $"/api/v1/routes/{routeId}");
        Assert.Equal(["Colombo Fort", "Kandy"], route.GetProperty("stops").EnumerateArray().Select(stop => stop.GetProperty("stopName").GetString()).ToArray()); // A
        Assert.Equal(centreId, route.GetProperty("centreId").GetGuid()); // A

        // ---- Staff of the centre are created through the API by the Admin, then sign in for real
        var manager = await CreateStaffAsync(admin, factory, "CentreManager", centreId);
        var fleetOfficer = await CreateStaffAsync(admin, factory, "FleetOfficer", centreId);
        var dispatcher = await CreateStaffAsync(admin, factory, "Dispatcher", centreId);

        // ---- Step 2, Component B (Fleet): the fleet officer registers an active vehicle at the centre
        var vehicleId = await CreateAsync(fleetOfficer, "/api/v1/vehicles", new
        {
            centreId, plateNumber = "JRN-1001", model = "Ashok Leyland Viking", type = "SemiLuxury", capacity = 52, isAccessible = true, status = "Active"
        }, "B: vehicle");
        var vehicle = await GetAsync(fleetOfficer, $"/api/v1/vehicles/{vehicleId}");
        Assert.Equal("Active", vehicle.GetProperty("status").GetString()); // B
        Assert.Equal("JRN-1001", vehicle.GetProperty("plateNumber").GetString()); // B

        // ---- Step 3, Component C (Dispatch): the centre manager creates a driver, the dispatcher schedules the trip
        var driverId = await CreateAsync(manager, "/api/v1/drivers", new
        {
            centreId, fullName = "Kamal Perera", email = "driver@journey.test", password = Password, phoneNumber = "0771234567",
            licenseNumber = "DL-JRN-1", status = "Active"
        }, "C: driver");
        var departure = DateTime.UtcNow.Date.AddDays(3).AddHours(4);
        var tripId = await CreateAsync(dispatcher, "/api/v1/trips", new
        {
            centreId, routeId, vehicleId, driverId, bayId, scheduledTime = departure, notes = "Passenger journey test"
        }, "C: trip");
        var scheduled = await TripAsync(dispatcher, tripId);
        Assert.Equal("Scheduled", scheduled.GetProperty("status").GetString()); // C
        Assert.Equal(vehicleId, scheduled.GetProperty("vehicleId").GetGuid()); // C
        Assert.Equal(driverId, scheduled.GetProperty("driverId").GetGuid()); // C
        Assert.Equal(bayId, scheduled.GetProperty("bayId").GetGuid()); // C

        // ---- Step 4, Component D (Fares): the centre manager sets the fare for the route
        await CreateAsync(manager, "/api/v1/fare-rules", new { routeId, amount = FareAmount }, "D: fare rule");

        // ---- Step 5, Component D (Passenger): the commuter registers, staff top up the wallet, the commuter books
        var register = AuthenticationTests.NewClient(factory);
        var registered = await register.PostAsJsonAsync("/api/v1/auth/register", new { name = "Nimal Commuter", email = "commuter@journey.test", password = Password });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode); // D
        Assert.Equal("Commuter", (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString()); // D
        register.Dispose();
        var commuter = await SignInAsync(factory, "commuter@journey.test");

        var passengers = await GetAsync(manager, "/api/v1/passengers?search=commuter@journey.test");
        Assert.Equal(1, passengers.GetArrayLength()); // D
        var passengerId = passengers[0].GetProperty("id").GetGuid();
        Assert.Equal(0m, await WalletBalanceAsync(manager, passengerId)); // D: new wallet is empty

        var topUp = await manager.PostAsJsonAsync($"/api/v1/passengers/{passengerId}/wallet/top-ups", new { amount = TopUpAmount });
        Assert.Equal(HttpStatusCode.Created, topUp.StatusCode); // D
        Assert.Equal(TopUpAmount, await WalletBalanceAsync(manager, passengerId)); // D

        var booked = await commuter.PostAsJsonAsync("/api/v1/bookings/me", new { tripId, passengerCount = 1 });
        Assert.True(booked.StatusCode == HttpStatusCode.Created, $"D: booking: {(int)booked.StatusCode} {await booked.Content.ReadAsStringAsync()}");
        var booking = await booked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(FareAmount, booking.GetProperty("fare").GetDecimal()); // D: charged the route fare
        Assert.Equal("Confirmed", booking.GetProperty("status").GetString()); // D
        var bookingId = booking.GetProperty("id").GetGuid();
        var qrCode = booking.GetProperty("qrCode").GetString()!;
        Assert.StartsWith("BKG-", qrCode); // D

        Assert.Equal(TopUpAmount - FareAmount, await WalletBalanceAsync(manager, passengerId)); // D: wallet debited by the fare

        var ticket = await SingleTicketAsync(commuter);
        Assert.Equal("Upcoming", ticket.GetProperty("ticketGroup").GetString()); // D
        Assert.True(ticket.GetProperty("canBoard").GetBoolean()); // D
        Assert.Equal(qrCode, ticket.GetProperty("qrCode").GetString()); // D: boarding QR is shown
        Assert.Equal("Scheduled", ticket.GetProperty("tripStatus").GetString()); // C seen by D
        Assert.Equal("Colombo", ticket.GetProperty("origin").GetString()); // A seen by D
        Assert.Equal("Kandy", ticket.GetProperty("destination").GetString()); // A seen by D
        Assert.Equal("B1", ticket.GetProperty("bay").GetString()); // A seen by D

        return new Journey(factory, admin, manager, fleetOfficer, dispatcher, commuter, centreId, routeId, tripId, passengerId, bookingId, qrCode);
    }

    // ---------------------------------------------------------------- tests

    [Fact]
    public async Task PassengerJourney_FromNetworkSetupToBookingToCompletedTrip_WorksAcrossAllComponents()
    {
        using var journey = await ArrangeAsync(); // steps 1 to 5

        // ---- Step 6, Component C (Dispatch): the dispatcher moves the trip Ready -> Boarding -> Dispatched -> Completed
        foreach (var status in new[] { "Ready", "Boarding", "Dispatched", "Completed" })
        {
            var response = await journey.Dispatcher.PatchAsJsonAsync($"/api/v1/trips/{journey.TripId}/status", new { status });
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"C: {status}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            var trip = await TripAsync(journey.Dispatcher, journey.TripId);
            Assert.Equal(status, trip.GetProperty("status").GetString()); // C

            if (status == "Boarding")
            {
                var boarding = await SingleTicketAsync(journey.Commuter);
                Assert.True(boarding.GetProperty("canBoard").GetBoolean()); // D: the QR is still valid while boarding
                Assert.Equal("Upcoming", boarding.GetProperty("ticketGroup").GetString()); // D
            }
            if (status == "Dispatched") Assert.NotEqual(JsonValueKind.Null, trip.GetProperty("actualDepartureAt").ValueKind); // C: departure time recorded
            if (status == "Completed") Assert.NotEqual(JsonValueKind.Null, trip.GetProperty("completedAt").ValueKind); // C: completion time recorded
        }

        // ---- Step 7, Component D (Passenger): the commuter's ticket is now in the Past group and the trip is Completed
        var ticket = await SingleTicketAsync(journey.Commuter);
        Assert.Equal("Past", ticket.GetProperty("ticketGroup").GetString()); // D
        Assert.Equal("Completed", ticket.GetProperty("tripStatus").GetString()); // C seen by D
        Assert.False(ticket.GetProperty("canBoard").GetBoolean()); // D: a finished trip cannot be boarded
        Assert.Equal(JsonValueKind.Null, ticket.GetProperty("qrCode").ValueKind); // D: no boarding QR after the trip
        Assert.Equal(TopUpAmount - FareAmount, await WalletBalanceAsync(journey.Manager, journey.PassengerId)); // D: the fare stays paid

        // C: the finished trip moves to the trip history and no longer accepts changes
        var history = await GetAsync(journey.Dispatcher, $"/api/v1/trips/history?centreId={journey.CentreId}");
        Assert.Contains(history.EnumerateArray(), trip => trip.GetProperty("id").GetGuid() == journey.TripId);
        var afterCompletion = await journey.Dispatcher.PatchAsJsonAsync($"/api/v1/trips/{journey.TripId}/status", new { status = "Ready" });
        Assert.Equal(HttpStatusCode.BadRequest, afterCompletion.StatusCode); // C
    }

    [Fact]
    public async Task CancelledTrip_ShowsACancelledTicketWithoutQr_AndStaffCanRefundTheBooking()
    {
        using var journey = await ArrangeAsync(); // steps 1 to 5

        // ---- Component C (Dispatch): the dispatcher cancels the trip with a reason
        var cancel = await journey.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{journey.TripId}")
        {
            Content = JsonContent.Create(new { reason = "Road closed by flooding" })
        });
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode); // C
        var trip = await TripAsync(journey.Dispatcher, journey.TripId);
        Assert.Equal("Cancelled", trip.GetProperty("status").GetString()); // C
        Assert.Equal("Road closed by flooding", trip.GetProperty("cancellationReason").GetString()); // C
        var history = await GetAsync(journey.Dispatcher, $"/api/v1/trips/history?centreId={journey.CentreId}");
        Assert.Contains(history.EnumerateArray(), item => item.GetProperty("id").GetGuid() == journey.TripId); // C: record retained

        // ---- Component D (Passenger): the ticket is cancelled and cannot be used to board
        var ticket = await SingleTicketAsync(journey.Commuter);
        Assert.Equal("Cancelled", ticket.GetProperty("ticketGroup").GetString()); // D
        Assert.Equal("Cancelled", ticket.GetProperty("tripStatus").GetString()); // C seen by D
        Assert.False(ticket.GetProperty("canBoard").GetBoolean()); // D
        Assert.Equal(JsonValueKind.Null, ticket.GetProperty("qrCode").ValueKind); // D: no boarding QR

        // ---- Component D: the existing refund rule is a staff cancellation of the booking (reversal, not deletion)
        var refund = await journey.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/bookings/{journey.BookingId}")
        {
            Content = JsonContent.Create(new { reason = "Trip cancelled by the centre" })
        });
        Assert.Equal(HttpStatusCode.NoContent, refund.StatusCode); // D
        Assert.Equal(TopUpAmount, await WalletBalanceAsync(journey.Manager, journey.PassengerId)); // D: the whole fare is back in the wallet
        var detail = await GetAsync(journey.Manager, $"/api/v1/bookings/{journey.BookingId}");
        Assert.Equal("Cancelled", detail.GetProperty("status").GetString()); // D
        Assert.Equal(FareAmount, detail.GetProperty("refundAmount").GetDecimal()); // D
        var types = detail.GetProperty("transactions").EnumerateArray().Select(t => t.GetProperty("type").GetString()).ToArray();
        Assert.Contains("Fare", types); // D: the original charge is kept
        Assert.Contains("Refund", types); // D: and the refund is recorded as its own transaction
    }

    [Fact]
    public async Task CancelledTrip_RefundsThePassengersWalletAutomatically()
    {
        // Intended rule: a passenger who was charged for a trip that the centre then cancels must get the money back.
        // SRS 4.3 "Payment history and refund handling"; COMPONENT_BREAKDOWN "Soft-cancel a booking; reverse rather than
        // delete financial records". The previous test shows the refund works when staff cancel the booking by hand.
        using var journey = await ArrangeAsync(); // steps 1 to 5
        Assert.Equal(TopUpAmount - FareAmount, await WalletBalanceAsync(journey.Manager, journey.PassengerId)); // D: charged

        var cancel = await journey.Dispatcher.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/trips/{journey.TripId}")
        {
            Content = JsonContent.Create(new { reason = "Road closed by flooding" })
        });
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode); // C

        // D: without any further staff action the passenger's money is returned and the booking is cancelled
        Assert.Equal(TopUpAmount, await WalletBalanceAsync(journey.Manager, journey.PassengerId));
        var detail = await GetAsync(journey.Manager, $"/api/v1/bookings/{journey.BookingId}");
        Assert.Equal("Cancelled", detail.GetProperty("status").GetString());
    }
}
