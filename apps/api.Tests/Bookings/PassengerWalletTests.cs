using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.Enums;
using api.Models;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Bookings;

/// <summary>Component D: passenger profiles and wallet top-ups.</summary>
public sealed class PassengerWalletTests
{
    private const string Url = "/api/v1/passengers";

    [Fact]
    public async Task Create_WithoutPortalAccount_CreatesPassengerAndEmptyWallet()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { fullName = " Nimal Perera ", phoneNumber = "077 123-4567", category = "Student" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("0771234567", body.GetProperty("phoneNumber").GetString());
        Assert.Equal(0m, body.GetProperty("balance").GetDecimal());
        Assert.Equal(1, await factory.CountAsync<Wallet>());
    }

    [Fact]
    public async Task Create_WithDuplicatePhoneNumber_ReturnsConflict()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");
        await client.PostAsJsonAsync(Url, new { fullName = "A", phoneNumber = "0771234567" });

        var response = await client.PostAsJsonAsync(Url, new { fullName = "B", phoneNumber = "077-123 4567" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await factory.CountAsync<Passenger>());
    }

    [Theory]
    [InlineData("", "0771234567")]
    [InlineData("Name", "")]
    public async Task Create_WithMissingNameOrPhone_ReturnsBadRequest(string name, string phone)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { fullName = name, phoneNumber = phone });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithPasswordButNoEmail_ReturnsBadRequest()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync(Url, new { fullName = "A", phoneNumber = "0771234567", password = "Password123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TopUp_AddsToBalance_AndRecordsTopupTransaction()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 100);
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync($"{Url}/{passenger.Id}/wallet/top-ups", new { amount = 250.50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(350.50m, body.GetProperty("balance").GetDecimal());
        var transaction = await factory.WithDbAsync(db => db.Transactions.SingleAsync());
        Assert.Equal(TransactionType.Topup, transaction.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task TopUp_WithZeroOrNegativeAmount_IsRejected_AndBalanceUnchanged(decimal amount)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 100);
        using var client = factory.CreateClientAs("CentreManager");

        var response = await client.PostAsJsonAsync($"{Url}/{passenger.Id}/wallet/top-ups", new { amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var balance = await factory.WithDbAsync(db => db.Wallets.Select(w => w.Balance).SingleAsync());
        Assert.Equal(100m, balance);
    }

    [Fact]
    public async Task Delete_DeactivatesPassenger_AndRestoreReactivates()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var (passenger, _) = await TestWorld.AddPassengerAsync(factory, world, 100);
        using var client = factory.CreateClientAs("CentreManager");

        await client.DeleteAsync($"{Url}/{passenger.Id}");
        var afterDelete = await factory.WithDbAsync(db => db.Passengers.FindAsync(passenger.Id).AsTask());
        await client.PostAsync($"{Url}/{passenger.Id}/restore", null);
        var afterRestore = await factory.WithDbAsync(db => db.Passengers.FindAsync(passenger.Id).AsTask());

        Assert.False(afterDelete!.IsActive);
        Assert.True(afterRestore!.IsActive);
    }

    [Fact]
    public async Task List_FiltersByCategoryAndSearch()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");
        await client.PostAsJsonAsync(Url, new { fullName = "Kamal Silva", phoneNumber = "0711111111", category = "Adult" });
        await client.PostAsJsonAsync(Url, new { fullName = "Sunil Student", phoneNumber = "0722222222", category = "Student" });

        var students = await client.GetFromJsonAsync<JsonElement>($"{Url}?category=Student");
        var bySearch = await client.GetFromJsonAsync<JsonElement>($"{Url}?search=kamal");

        Assert.Equal(1, students.GetArrayLength());
        Assert.Equal("Sunil Student", students[0].GetProperty("fullName").GetString());
        Assert.Equal(1, bySearch.GetArrayLength());
    }

    [Fact]
    public async Task Get_UnknownPassenger_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("CentreManager");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{Guid.NewGuid()}")).StatusCode);
    }
}
