using api.Data;
using api.Enums;
using api.Models;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Route = api.Models.Route;

namespace api.Tests.Database;

/// <summary>Entity builders and assertion helpers for the PostgreSQL tests.</summary>
internal static class PgData
{
    public static Centre Centre(string code = "CMB") => new()
    {
        Code = code, Name = $"{code} Centre", City = "Colombo", District = "Colombo", Status = CentreStatus.Operating
    };

    public static Route Route(Centre centre, string number = "01") => new()
    {
        CentreId = centre.Id, RouteNumber = number, Name = "Test Route", Origin = "A", Destination = "B",
        DistanceKm = 50, EstimatedDurationMin = 60
    };

    public static Vehicle Vehicle(Centre centre, string plate = "NB-1000") =>
        new() { CentreId = centre.Id, PlateNumber = plate, Model = "Bus", Capacity = 40 };

    public static Driver Driver(Centre centre, string license = "LIC-1") =>
        new() { CentreId = centre.Id, FullName = "Driver", LicenseNumber = license };

    public static Bay Bay(Centre centre, string code = "B1") => new() { CentreId = centre.Id, Code = code };

    public static Passenger Passenger(string phone = "0771234567") =>
        new() { FullName = "Passenger", PhoneNumber = phone };

    /// <summary>A saved centre with a route, vehicle, driver, bay, trip, passenger, wallet and fare rule.</summary>
    public record Graph(Centre Centre, Route Route, Vehicle Vehicle, Driver Driver, Bay Bay, Trip Trip, Passenger Passenger, Wallet Wallet);

    public static async Task<Graph> SeedGraphAsync(AppDbContext db, decimal balance = 500m)
    {
        var centre = Centre();
        var route = Route(centre);
        var vehicle = Vehicle(centre);
        var driver = Driver(centre);
        var bay = Bay(centre);
        var passenger = Passenger();
        var wallet = new Wallet { PassengerId = passenger.Id, Balance = balance };
        var trip = new Trip
        {
            CentreId = centre.Id, RouteId = route.Id, VehicleId = vehicle.Id, DriverId = driver.Id, BayId = bay.Id,
            ScheduledTime = DateTime.UtcNow.Date.AddDays(3).AddHours(4)
        };
        db.AddRange(centre, route, vehicle, driver, bay, passenger);
        await db.SaveChangesAsync();
        db.AddRange(wallet, trip, new FareRule { RouteId = route.Id, Amount = 100m });
        await db.SaveChangesAsync();
        return new Graph(centre, route, vehicle, driver, bay, trip, passenger, wallet);
    }

    public static Booking Booking(Graph g, string seat = "S1", BookingStatus status = BookingStatus.Confirmed) => new()
    {
        TripId = g.Trip.Id, PassengerId = g.Passenger.Id, SeatNumber = seat, QrCode = $"BKG-{Guid.NewGuid():N}",
        Fare = 100m, Status = status
    };

    /// <summary>Asserts that saving throws a database error with the given PostgreSQL SQLSTATE code.</summary>
    public static async Task AssertSqlStateAsync(string expectedSqlState, Func<Task> action)
    {
        var exception = await Xunit.Assert.ThrowsAnyAsync<Exception>(action);
        var postgres = Find<PostgresException>(exception);
        Xunit.Assert.True(postgres is not null, $"Expected a PostgresException but got: {exception}");
        Xunit.Assert.Equal(expectedSqlState, postgres!.SqlState);
    }

    private static T? Find<T>(Exception? exception) where T : Exception
    {
        while (exception is not null)
        {
            if (exception is T match) return match;
            exception = exception.InnerException;
        }
        return null;
    }

    public static async Task<string?> ScalarAsync(AppDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
