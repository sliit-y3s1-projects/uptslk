using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.DTO.Notifications;
using api.Entities.Enums;
using api.Interfaces.Services;
using api.Tests.Shared;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests.Notifications;

/// <summary>The SignalR hub with real JWT authentication, connecting the way the Flutter client does (token in the query string).</summary>
public sealed class NotificationHubTests
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Hub_AcceptsQueryStringToken_AndDeliversOnlyToThatUser()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        var userId = await factory.SeedUserAsync("rider@test.lk", Password, UserRole.Commuter);
        await using var connection = Connect(factory, await LoginAsync(factory, "rider@test.lk"));
        var received = new TaskCompletionSource<NotificationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<NotificationResponse>("notification", received.SetResult);
        await connection.StartAsync();

        var publisher = factory.Services.GetRequiredService<INotificationPublisher>();
        await publisher.PublishAsync(Guid.NewGuid(), Notification("Someone else"), default);
        await publisher.PublishAsync(userId, Notification("Bay changed"), default);

        var notification = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Bay changed", notification.Title);
        Assert.Equal(NotificationType.TripUpdated, notification.Type);
    }

    [Fact]
    public async Task Hub_RejectsStaffAndAnonymousConnections()
    {
        using var factory = await RealAuthApiFactory.CreateAsync();
        await factory.SeedUserAsync("dispatcher@test.lk", Password, UserRole.Dispatcher);

        await using var staff = Connect(factory, await LoginAsync(factory, "dispatcher@test.lk"));
        await using var anonymous = Connect(factory, null);

        await Assert.ThrowsAnyAsync<Exception>(() => staff.StartAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => anonymous.StartAsync());
    }

    private static NotificationResponse Notification(string title) =>
        new(Guid.NewGuid(), NotificationType.TripUpdated, title, "Message", Guid.NewGuid(), null, null, DateTime.UtcNow);

    private static async Task<string> LoginAsync(RealAuthApiFactory factory, string email)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static HubConnection Connect(RealAuthApiFactory factory, string? token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/notifications"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var uri = token is null ? context.Uri : new Uri($"{context.Uri}?access_token={token}");
                    return await factory.Server.CreateWebSocketClient().ConnectAsync(uri, cancellationToken);
                };
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();
}
