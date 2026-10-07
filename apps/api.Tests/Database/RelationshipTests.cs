using api.Data;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Database;

/// <summary>Database testing: relationships, cascade/set-null delete rules and data integrity on PostgreSQL.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Database")]
public sealed class RelationshipTests(PostgresFixture postgres)
{
    private async Task<(AppDbContext Db, PgData.Graph Graph)> ArrangeAsync()
    {
        var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());
        return (db, await PgData.SeedGraphAsync(db));
    }

    [Fact]
    public async Task Trip_LoadsItsFullAssignmentGraph()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        var trip = await reader.Trips.Include(t => t.Route).Include(t => t.Vehicle).Include(t => t.Driver).Include(t => t.Bay)
            .Include(t => t.Centre).SingleAsync(t => t.Id == g.Trip.Id);

        Assert.Equal(g.Route.RouteNumber, trip.Route.RouteNumber);
        Assert.Equal(g.Vehicle.PlateNumber, trip.Vehicle.PlateNumber);
        Assert.Equal(g.Driver.LicenseNumber, trip.Driver.LicenseNumber);
        Assert.Equal(g.Bay.Code, trip.Bay.Code);
        Assert.Equal(g.Centre.Code, trip.Centre.Code);
    }

    [Fact]
    public async Task Centre_ExposesItsBaysRoutesAndVehicles()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        var centre = await reader.Centres.Include(c => c.Bays).Include(c => c.Routes).Include(c => c.Vehicles)
            .SingleAsync(c => c.Id == g.Centre.Id);

        Assert.Single(centre.Bays);
        Assert.Single(centre.Routes);
        Assert.Single(centre.Vehicles);
    }

    [Fact]
    public async Task Passenger_And_Wallet_AreOneToOne()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        var passenger = await reader.Passengers.Include(p => p.Wallet).SingleAsync(p => p.Id == g.Passenger.Id);

        Assert.NotNull(passenger.Wallet);
        Assert.Equal(500m, passenger.Wallet!.Balance);
    }

    [Fact]
    public async Task SecondWallet_ForTheSamePassenger_IsRejected()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;

        await using var second = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        second.Add(new Wallet { PassengerId = g.Passenger.Id, Balance = 1m });

        await PgData.AssertSqlStateAsync("23505", () => second.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingAPassengerWithoutBookings_CascadesToWalletAndTransactions()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        db.Add(new Transaction { WalletId = g.Wallet.Id, Type = TransactionType.Topup, Amount = 50m });
        await db.SaveChangesAsync();

        db.Remove(await db.Passengers.SingleAsync(p => p.Id == g.Passenger.Id));
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.Wallets.CountAsync());
        Assert.Equal(0, await db.Transactions.CountAsync());
    }

    [Fact]
    public async Task DeletingARoute_CascadesToItsStopsAndSchedules_ButNotItsTrips()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        db.Add(new RouteStop { RouteId = g.Route.Id, StopName = "S1", SequenceOrder = 1 });
        db.Add(new RouteSchedule { RouteId = g.Route.Id, BayId = g.Bay.Id, FirstDeparture = new TimeOnly(5, 0), LastDeparture = new TimeOnly(20, 0), HeadwayMinutes = 30, OperatingDays = "Mon" });
        await db.SaveChangesAsync();

        // The trip and fare rule reference the route with restrict rules, so PostgreSQL refuses the delete.
        await PgData.AssertSqlStateAsync("23503", () => db.Routes.Where(r => r.Id == g.Route.Id).ExecuteDeleteAsync());
        Assert.Equal(1, await db.RouteStops.CountAsync());

        await db.Trips.ExecuteDeleteAsync();
        await db.FareRules.ExecuteDeleteAsync();
        await db.Routes.Where(r => r.Id == g.Route.Id).ExecuteDeleteAsync();

        Assert.Equal(0, await db.RouteStops.CountAsync());
        Assert.Equal(0, await db.RouteSchedules.CountAsync());
    }

    [Fact]
    public async Task DeletingABooking_KeepsItsWalletTransaction_WithNullBookingId()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        var booking = PgData.Booking(g);
        db.Add(booking);
        await db.SaveChangesAsync();
        db.Add(new Transaction { WalletId = g.Wallet.Id, BookingId = booking.Id, Type = TransactionType.Fare, Amount = 100m });
        await db.SaveChangesAsync();

        db.Remove(await db.Bookings.SingleAsync());
        await db.SaveChangesAsync();

        var transaction = await db.Transactions.SingleAsync();
        Assert.Null(transaction.BookingId);
        Assert.Equal(100m, transaction.Amount);
    }

    [Fact]
    public async Task DeletingATrip_KeepsItsIncidents_WithNullTripId()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        db.Add(new Incident
        {
            CentreId = g.Centre.Id, TripId = g.Trip.Id, ReportedByName = "Driver", Type = IncidentType.Breakdown,
            Title = "Engine", Description = "Stopped", SlaDueAt = DateTime.UtcNow.AddHours(1)
        });
        await db.SaveChangesAsync();

        db.Remove(await db.Trips.SingleAsync());
        await db.SaveChangesAsync();

        var incident = await db.Incidents.SingleAsync();
        Assert.Null(incident.TripId);
    }

    [Fact]
    public async Task MaintenanceRecord_RequiresAnExistingVehicle()
    {
        var (db, _) = await ArrangeAsync();
        await using var _ = db;

        db.Add(new MaintenanceRecord { VehicleId = Guid.NewGuid(), Type = "Service", Description = "x", ScheduledFor = DateTime.UtcNow });

        await PgData.AssertSqlStateAsync("23503", () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Enums_AreStoredAndReadBack()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        db.Add(PgData.Booking(g, "S5", BookingStatus.Completed));
        await db.SaveChangesAsync();
        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        var booking = await reader.Bookings.SingleAsync();

        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    [Fact]
    public async Task UtcDateTimes_RoundTripUnchanged_ThroughTimestamptz()
    {
        var (db, g) = await ArrangeAsync();
        await using var _ = db;
        var expected = new DateTime(2026, 12, 25, 3, 30, 15, DateTimeKind.Utc);
        (await db.Trips.SingleAsync()).ScheduledTime = expected;
        await db.SaveChangesAsync();
        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        var actual = (await reader.Trips.SingleAsync()).ScheduledTime;

        Assert.Equal(expected, actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }
}
