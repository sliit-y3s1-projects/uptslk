using Supabase.Storage;

namespace api.Services;

public interface IImageStorageService
{
    Task<string> UploadProfileAsync(
        Guid userId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> UploadVehicleAsync(
        Guid vehicleId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);
}

public sealed class SupabaseImageStorageService(
    IConfiguration configuration,
    ILogger<SupabaseImageStorageService> logger) : IImageStorageService
{
    private const int MaxFileSize = 5 * 1024 * 1024;
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    private Supabase.Client? _client;

    public Task<string> UploadProfileAsync(
        Guid userId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default) =>
        UploadAsync(
            configuration["Supabase:Storage:ProfileImagesBucket"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_PROFILE_IMAGES_BUCKET")
                ?? "avatars",
            $"profiles/{userId:N}/avatar",
            content,
            contentType,
            cancellationToken);

    public Task<string> UploadVehicleAsync(
        Guid vehicleId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default) =>
        UploadAsync(
            configuration["Supabase:Storage:VehicleImagesBucket"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_VEHICLE_IMAGES_BUCKET")
                ?? "vehicles",
            $"vehicles/{vehicleId:N}/primary",
            content,
            contentType,
            cancellationToken);

    private async Task<string> UploadAsync(
        string bucketName,
        string objectPath,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (content.Length is 0 or > MaxFileSize)
            throw new InvalidDataException("Images must be between 1 byte and 5 MB.");

        var normalizedContentType = contentType.Trim().ToLowerInvariant();
        if (!HasValidSignature(content, normalizedContentType))
            throw new InvalidDataException("Upload a valid JPG, PNG, or WebP image.");

        var client = await GetClientAsync();
        await client.Storage.From(bucketName).Upload(
            content,
            objectPath,
            new Supabase.Storage.FileOptions
            {
                CacheControl = "3600",
                ContentType = normalizedContentType,
                Upsert = true
            },
            null,
            false,
            cancellationToken);

        var publicUrl = client.Storage.From(bucketName).GetPublicUrl(objectPath);
        var separator = publicUrl.Contains('?') ? '&' : '?';
        var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        logger.LogInformation("Updated Supabase image object {ObjectPath}", objectPath);
        return $"{publicUrl}{separator}v={version}";
    }

    private async Task<Supabase.Client> GetClientAsync()
    {
        if (_client is not null) return _client;

        await _clientLock.WaitAsync();
        try
        {
            if (_client is not null) return _client;

            var url = configuration["Supabase:Url"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = configuration["Supabase:SecretKey"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY")
                ?? configuration["Supabase:ServiceRoleKey"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
                ?? Environment.GetEnvironmentVariable("SUPABASE_KEY");

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    "Supabase storage is not configured. Set SUPABASE_URL and SUPABASE_SECRET_KEY.");

            var client = new Supabase.Client(url, key, new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false,
                AutoRefreshToken = false
            });
            await client.InitializeAsync();
            _client = client;
            return client;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    private static bool HasValidSignature(byte[] content, string contentType) =>
        contentType switch
        {
            "image/jpeg" => content.Length >= 3
                && content[0] == 0xFF
                && content[1] == 0xD8
                && content[2] == 0xFF,
            "image/png" => content.Length >= 8
                && content[0] == 0x89
                && content[1] == 0x50
                && content[2] == 0x4E
                && content[3] == 0x47
                && content[4] == 0x0D
                && content[5] == 0x0A
                && content[6] == 0x1A
                && content[7] == 0x0A,
            "image/webp" => content.Length >= 12
                && content.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && content.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
}
