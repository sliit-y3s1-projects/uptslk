using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using api.Entities.Enums;
using api.Interfaces.Services;
using api.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Supabase.Storage.Exceptions;
using Xunit;

namespace api.Tests.Fleet;

/// <summary>Component B (Fleet and Maintenance): vehicle image upload, and the failure branches of update and deactivate.</summary>
public sealed class VehicleImageAndUpdateTests
{
    private const string Url = "/api/v1/vehicles";
    private const int MaxImageBytes = 5 * 1024 * 1024;

    private static async Task<(TestApiFactory Factory, FakeImageStorage Storage)> CreateFactoryAsync()
    {
        var storage = new FakeImageStorage();
        var factory = new TestApiFactory(services =>
        {
            services.RemoveAll<IImageStorageService>();
            services.AddSingleton<IImageStorageService>(storage);
        });
        await factory.InitializeDatabaseAsync();
        return (factory, storage);
    }

    private static MultipartFormDataContent ImageForm(byte[] content, string contentType = "image/png")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", "bus.png" } };
    }

    private static object VehicleUpdate(Guid centreId, string plate = "NB-1000", int capacity = 40) => new
    {
        centreId, plateNumber = plate, model = "Lanka Ashok Leyland", type = "Normal", capacity, isAccessible = false, status = "Active"
    };

    // ---- image upload ----

    [Fact]
    public async Task UploadImage_AsFleetOfficerOfTheCentre_StoresTheImageAndSavesItsUrl()
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);
        using var form = ImageForm([1, 2, 3, 4]);

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var upload = Assert.Single(storage.Uploads);
        Assert.Equal(world.Vehicle.Id, upload.Id);
        Assert.Equal("image/png", upload.ContentType);
        Assert.Equal(4, upload.Length);
        var profile = await client.GetFromJsonAsync<JsonElement>($"{Url}/{world.Vehicle.Id}");
        Assert.Equal($"https://storage.test/vehicle/{world.Vehicle.Id}.png", profile.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task UploadImage_OfExactlyFiveMegabytes_IsAccepted()
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);
        using var form = ImageForm(new byte[MaxImageBytes]);

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MaxImageBytes, Assert.Single(storage.Uploads).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MaxImageBytes + 1)]
    public async Task UploadImage_EmptyOrOverFiveMegabytes_IsRejected_WithoutCallingStorage(int size)
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);
        using var form = ImageForm(new byte[size]);

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(storage.Uploads);
        var vehicle = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Null(vehicle!.ImageUrl);
    }

    [Theory]
    [MemberData(nameof(StorageFailures))]
    public async Task UploadImage_StorageProblems_MapToTheRightHttpStatus_AndKeepNoUrl(Exception failure, HttpStatusCode expected)
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        storage.FailWith = failure;
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);
        using var form = ImageForm([1, 2, 3]);

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(expected, response.StatusCode);
        var vehicle = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Null(vehicle!.ImageUrl);
    }

    public static TheoryData<Exception, HttpStatusCode> StorageFailures() => new()
    {
        { new InvalidDataException("Choose a JPG, PNG, or WebP vehicle image."), HttpStatusCode.BadRequest },
        { new InvalidOperationException("Supabase is not configured."), HttpStatusCode.ServiceUnavailable },
        { new SupabaseStorageException("Bucket unavailable."), HttpStatusCode.BadGateway }
    };

    [Fact]
    public async Task UploadImage_ForUnknownVehicle_ReturnsNotFound()
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        using var client = factory.CreateClientAs("Admin");
        using var form = ImageForm([1, 2, 3]);

        var response = await client.PostAsync($"{Url}/{Guid.NewGuid()}/image", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task UploadImage_ByFleetOfficerOfAnotherCentre_IsForbidden_AndNothingIsStored()
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: Guid.NewGuid());
        using var form = ImageForm([1, 2, 3]);

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(storage.Uploads);
    }

    [Fact]
    public async Task UploadImage_WithoutLogin_ReturnsUnauthorized()
    {
        var (factory, storage) = await CreateFactoryAsync();
        using var _ = factory;
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateAnonymousClient();
        using var form = ImageForm([1, 2, 3]);

        var response = await client.PostAsync($"{Url}/{world.Vehicle.Id}/image", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(storage.Uploads);
    }

    // ---- update and deactivate failure branches ----

    [Fact]
    public async Task Update_UnknownVehicle_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}", VehicleUpdate(world.Centre.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MovingToAnUnknownCentre_ReturnsBadRequest_AndKeepsTheVehicle()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("Admin");

        var response = await client.PutAsJsonAsync($"{Url}/{world.Vehicle.Id}", VehicleUpdate(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var vehicle = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Equal(world.Centre.Id, vehicle!.CentreId);
        Assert.Equal(world.Vehicle.PlateNumber, vehicle.PlateNumber);
    }

    [Fact]
    public async Task Update_MovingToACentreTheOfficerDoesNotManage_IsForbidden()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        var other = await TestWorld.SeedAsync(factory, "OTHER", "NB-9999", "LIC-OTHER");
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{world.Vehicle.Id}", VehicleUpdate(other.Centre.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var vehicle = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Equal(world.Centre.Id, vehicle!.CentreId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public async Task Update_WithCapacityOnTheBoundary_IsAccepted(int capacity)
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: world.Centre.Id);

        var response = await client.PutAsJsonAsync($"{Url}/{world.Vehicle.Id}", VehicleUpdate(world.Centre.Id, capacity: capacity));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var vehicle = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Equal(capacity, vehicle!.Capacity);
    }

    [Fact]
    public async Task Deactivate_UnknownVehicle_ReturnsNotFound()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        using var client = factory.CreateClientAs("Admin");

        var response = await client.DeleteAsync($"{Url}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_ByFleetOfficerOfAnotherCentre_IsForbidden_AndTheVehicleStaysActive()
    {
        using var factory = await TestApiExtensions.CreateInitializedFactoryAsync();
        var world = await TestWorld.SeedAsync(factory);
        using var client = factory.CreateClientAs("FleetOfficer", centreId: Guid.NewGuid());

        var response = await client.DeleteAsync($"{Url}/{world.Vehicle.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var vehicle = await factory.WithDbAsync(db => db.Vehicles.FindAsync(world.Vehicle.Id).AsTask());
        Assert.Equal(VehicleStatus.Active, vehicle!.Status);
    }
}
