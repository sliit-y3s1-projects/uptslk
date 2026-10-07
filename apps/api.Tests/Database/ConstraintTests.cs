using api.Data;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Database;

/// <summary>Database testing: unique, foreign-key, not-null, length and precision constraints enforced by PostgreSQL.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Database")]
public sealed class ConstraintTests(PostgresFixture postgres)
{
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";
    private const string NotNullViolation = "23502";
    private const string StringTooLong = "22001";
    private const string NumericOverflow = "22003";

    private async Task<AppDbContext> NewDbAsync() =>
        PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());

    // ---- unique constraints ----

    [Fact]
    public async Task Centre_Code_MustBeUnique()
    {
        await using var db = await NewDbAsync();
        db.Add(PgData.Centre("KUR"));
        await db.SaveChangesAsync();

        db.Add(PgData.Centre("KUR"));

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Vehicle_PlateNumber_MustBeUnique()
    {
        await using var db = await NewDbAsync();
        var centre = PgData.Centre();
        db.AddRange(centre, PgData.Vehicle(centre, "AB-1"));
        await db.SaveChangesAsync();

        db.Add(PgData.Vehicle(centre, "AB-1"));

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Driver_LicenseNumber_MustBeUnique()
    {
        await using var db = await NewDbAsync();
        var centre = PgData.Centre();
        db.AddRange(centre, PgData.Driver(centre, "L-1"));
        await db.SaveChangesAsync();

        db.Add(PgData.Driver(centre, "L-1"));

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Passenger_PhoneNumber_MustBeUnique()
    {
        await using var db = await NewDbAsync();
        db.Add(PgData.Passenger("0771111111"));
        await db.SaveChangesAsync();

        db.Add(PgData.Passenger("0771111111"));

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Bay_Code_IsUniquePerCentre_ButReusableAtAnotherCentre()
    {
        await using var db = await NewDbAsync();
        var a = PgData.Centre("AAA");
        var b = PgData.Centre("BBB");
        db.AddRange(a, b, PgData.Bay(a, "B1"));
        await db.SaveChangesAsync();

        db.Add(PgData.Bay(b, "B1")); // same code, other centre: allowed
        await db.SaveChangesAsync();
        db.Add(PgData.Bay(a, "B1")); // same code, same centre: rejected

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Route_Number_IsUniquePerCentre()
    {
        await using var db = await NewDbAsync();
        var centre = PgData.Centre();
        db.AddRange(centre, PgData.Route(centre, "01"));
        await db.SaveChangesAsync();

        db.Add(PgData.Route(centre, "01"));

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task FareRule_AllowsOnlyOneRulePerRoute()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);

        db.Add(new FareRule { RouteId = g.Route.Id, Amount = 250m, IsActive = false });

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ActiveBookings_CannotShareASeatOnTheSameTrip()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);
        db.Add(PgData.Booking(g, "S1", BookingStatus.Confirmed));
        await db.SaveChangesAsync();

        db.Add(PgData.Booking(g, "S1", BookingStatus.Pending));

        await PgData.AssertSqlStateAsync(UniqueViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CancelledBooking_FreesItsSeatForANewBooking()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);
        db.Add(PgData.Booking(g, "S1", BookingStatus.Cancelled));
        await db.SaveChangesAsync();

        db.Add(PgData.Booking(g, "S1", BookingStatus.Confirmed));
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Bookings.CountAsync());
    }

    // ---- foreign keys ----

    [Fact]
    public async Task Trip_WithUnknownVehicle_IsRejected()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);

        db.Add(new Trip
        {
            CentreId = g.Centre.Id, RouteId = g.Route.Id, VehicleId = Guid.NewGuid(), DriverId = g.Driver.Id, BayId = g.Bay.Id,
            ScheduledTime = DateTime.UtcNow
        });

        await PgData.AssertSqlStateAsync(ForeignKeyViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Booking_WithUnknownPassenger_IsRejected()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);

        db.Add(new Booking { TripId = g.Trip.Id, PassengerId = Guid.NewGuid(), SeatNumber = "S9", QrCode = "BKG-X" });

        await PgData.AssertSqlStateAsync(ForeignKeyViolation, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Vehicle_WithUnknownCentre_IsRejected()
    {
        await using var db = await NewDbAsync();

        db.Add(new Vehicle { CentreId = Guid.NewGuid(), PlateNumber = "ZZ-1", Model = "Bus", Capacity = 10 });

        await PgData.AssertSqlStateAsync(ForeignKeyViolation, () => db.SaveChangesAsync());
    }

    // ---- delete restrictions (history must be retained) ----

    [Fact]
    public async Task Centre_WithBays_CannotBeDeleted()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);

        await PgData.AssertSqlStateAsync(ForeignKeyViolation,
            () => db.Centres.Where(c => c.Id == g.Centre.Id).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task Vehicle_WithTrips_CannotBeDeleted()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);


        await PgData.AssertSqlStateAsync(ForeignKeyViolation,
            () => db.Vehicles.Where(v => v.Id == g.Vehicle.Id).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task Trip_WithBookings_CannotBeDeleted()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);
        db.Add(PgData.Booking(g));
        await db.SaveChangesAsync();


        await PgData.AssertSqlStateAsync(ForeignKeyViolation,
            () => db.Trips.Where(t => t.Id == g.Trip.Id).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task Passenger_WithBookings_CannotBeDeleted()
    {
        await using var db = await NewDbAsync();
        var g = await PgData.SeedGraphAsync(db);
        db.Add(PgData.Booking(g));
        await db.SaveChangesAsync();


        await PgData.AssertSqlStateAsync(ForeignKeyViolation,
            () => db.Passengers.Where(p => p.Id == g.Passenger.Id).ExecuteDeleteAsync());
    }

    // ---- nullability, length, precision ----

    [Fact]
    public async Task Centre_Name_CannotBeNull()
    {
        await using var db = await NewDbAsync();
        db.Add(PgData.Centre());
        await db.SaveChangesAsync();

        await PgData.AssertSqlStateAsync(NotNullViolation,
            () => db.Database.ExecuteSqlRawAsync("UPDATE \"Centres\" SET \"Name\" = NULL"));
    }

    [Fact]
    public async Task Centre_Name_LongerThan160Characters_IsRejected()
    {
        await using var db = await NewDbAsync();
        var centre = PgData.Centre();
        centre.Name = new string('x', 161);
        db.Add(centre);

        await PgData.AssertSqlStateAsync(StringTooLong, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Centre_Name_Of160Characters_IsAccepted()
    {
        await using var db = await NewDbAsync();
        var centre = PgData.Centre();
        centre.Name = new string('x', 160);
        db.Add(centre);

        await db.SaveChangesAsync();

        Assert.Equal(160, (await db.Centres.SingleAsync()).Name.Length);
    }

    [Fact]
    public async Task Wallet_Balance_AboveNumeric12x2Range_IsRejected()
    {
        await using var db = await NewDbAsync();
        var passenger = PgData.Passenger();
        db.Add(passenger);
        await db.SaveChangesAsync();

        db.Add(new Wallet { PassengerId = passenger.Id, Balance = 10_000_000_000m });

        await PgData.AssertSqlStateAsync(NumericOverflow, () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Money_IsRoundedToTwoDecimalPlaces()
    {
        await using var db = await NewDbAsync();
        var passenger = PgData.Passenger();
        db.Add(passenger);
        await db.SaveChangesAsync();
        db.Add(new Wallet { PassengerId = passenger.Id, Balance = 10.126m });
        await db.SaveChangesAsync();

        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        Assert.Equal(10.13m, (await reader.Wallets.SingleAsync()).Balance);
    }
}
