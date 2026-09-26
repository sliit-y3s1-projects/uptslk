# Supabase C# Setup and Storage

This guide covers installing the Supabase .NET package, initializing a client, defining models, and using Supabase Storage.

## UPTSLK image-storage setup

The UPTSLK API uses Supabase Storage for profile and vehicle images. PostgreSQL remains the source of truth for application records; it stores only the resulting public URLs in `AspNetUsers.ProfilePhotoUrl` and `Vehicles.ImageUrl`.

### 1. Create the bucket

In the Supabase dashboard, open **Storage** and create two public buckets:

- `avatars` for user profile images
- `vehicles` for each vehicle's primary image

The buckets must be public so browsers can render the URLs returned by the API. Uploads still go through the API using the server-side secret key, so that key must never be added to Vite variables or sent to the browser.

If different bucket names are preferred, set `SUPABASE_PROFILE_IMAGES_BUCKET` and `SUPABASE_VEHICLE_IMAGES_BUCKET` accordingly.

### 2. Configure the API

Linux or macOS:

```bash
export SUPABASE_URL="https://YOUR_PROJECT.supabase.co"
export SUPABASE_SECRET_KEY="YOUR_SB_SECRET_KEY"
export SUPABASE_PROFILE_IMAGES_BUCKET="avatars"
export SUPABASE_VEHICLE_IMAGES_BUCKET="vehicles"
```

Windows PowerShell:

```powershell
$env:SUPABASE_URL="https://YOUR_PROJECT.supabase.co"
$env:SUPABASE_SECRET_KEY="YOUR_SB_SECRET_KEY"
$env:SUPABASE_PROFILE_IMAGES_BUCKET="avatars"
$env:SUPABASE_VEHICLE_IMAGES_BUCKET="vehicles"
```

For Choreo, add the same four names as runtime configurations and mark `SUPABASE_SECRET_KEY` as a secret.

For local .NET user secrets, the equivalent configuration keys are:

```bash
cd apps/api
dotnet user-secrets set "Supabase:Url" "https://YOUR_PROJECT.supabase.co"
dotnet user-secrets set "Supabase:SecretKey" "YOUR_SB_SECRET_KEY"
dotnet user-secrets set "Supabase:Storage:ProfileImagesBucket" "avatars"
dotnet user-secrets set "Supabase:Storage:VehicleImagesBucket" "vehicles"
```

### 3. Upload behavior

- `POST /api/v1/auth/me/profile-photo` accepts authenticated `multipart/form-data` with a field named `file`.
- JPG, PNG, and WebP images up to 5 MB are accepted.
- The API verifies the file signature in addition to the reported MIME type.
- Images use the stable object path `profiles/{userId}/avatar` with Supabase upsert enabled. A later upload replaces the existing profile image.
- The API stores a cache-busted public URL in PostgreSQL so the replacement appears immediately.
- The Supabase secret key (or legacy service-role key) is used only by the API. The frontend never connects directly to Supabase.

Vehicle images follow the same validation rules:

- `POST /api/v1/vehicles/{vehicleId}/image` accepts authenticated `multipart/form-data` with a field named `file`.
- Each vehicle has one primary image at `vehicles/{vehicleId}/primary`. Uploading another image replaces it rather than creating a gallery.
- Only an administrator, or authorized fleet staff belonging to the vehicle's centre, can upload it.
- The cache-busted public URL is stored in `Vehicles.ImageUrl` and returned by the vehicle list and detail endpoints.

New Supabase projects should use an `sb_secret_...` secret key. The API also accepts the legacy `service_role` key through `SUPABASE_SERVICE_ROLE_KEY` for older projects.

## Installing

### Install from NuGet

Install the [Supabase package from NuGet](https://www.nuget.org/packages/supabase/) from the directory that contains your `.csproj` file:

```bash
dotnet add package supabase
```

To install a specific version:

```bash
dotnet add package supabase --version <version>
```

Restore dependencies when needed:

```bash
dotnet restore
```

## Initializing

Find your project URL and public key in the Supabase dashboard, then pass them to the client initializer. Store these values in environment variables or local development configuration rather than committing them to source control.

```csharp
var url = Environment.GetEnvironmentVariable("SUPABASE_URL")
    ?? throw new InvalidOperationException("SUPABASE_URL is not configured.");
var key = Environment.GetEnvironmentVariable("SUPABASE_KEY")
    ?? throw new InvalidOperationException("SUPABASE_KEY is not configured.");

var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true
};

var supabase = new Supabase.Client(url, key, options);
await supabase.InitializeAsync();
```

## Defining Models

Supabase models should derive from `BaseModel`. Use the `Table`, `PrimaryKey`, and `Column` attributes when database names differ from their C# names.

```csharp
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("messages")]
public class Message : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; } = string.Empty;

    [Column("channel_id")]
    public int ChannelId { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is Message message && Id == message.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}
```

## Working with Models

```csharp
// Get all messages.
var response = await supabase.From<Message>().Get();
List<Message> models = response.Models;

// Insert a message.
var newMessage = new Message
{
    UserName = "acupofjose",
    ChannelId = 1
};
await supabase.From<Message>().Insert(newMessage);

// Update a message.
var model = response.Models.First();
model.UserName = "elrhomariyounes";
await model.Update<Message>();

// Delete a message.
await response.Models.Last().Delete<Message>();
```

## Storage

Each operation below lists the policy permissions required by Supabase Storage.

### List all buckets

Retrieves the details of all Storage buckets within an existing project.

**Required policy permissions:** `buckets: select`; `objects: none`

```csharp
var buckets = await supabase.Storage.ListBuckets();
```

### Retrieve a bucket

Retrieves the details of an existing Storage bucket.

**Required policy permissions:** `buckets: select`; `objects: none`

```csharp
var bucket = await supabase.Storage.GetBucket("avatars");
```

### Create a bucket

Creates a new Storage bucket.

**Required policy permissions:** `buckets: insert`; `objects: none`

```csharp
var bucket = await supabase.Storage.CreateBucket("avatars");
```

### Empty a bucket

Removes all objects inside a single bucket.

**Required policy permissions:** `buckets: select`; `objects: select and delete`

```csharp
var bucket = await supabase.Storage.EmptyBucket("avatars");
```

### Update a bucket

Updates an existing Storage bucket.

**Required policy permissions:** `buckets: update`; `objects: none`

```csharp
var bucket = await supabase.Storage.UpdateBucket(
    "avatars",
    new BucketUpsertOptions { Public = false });
```

### Delete a bucket

Deletes an existing Storage bucket. A bucket cannot be deleted while it contains objects; empty it first.

**Required policy permissions:** `buckets: select and delete`; `objects: none`

```csharp
var result = await supabase.Storage.DeleteBucket("avatars");
```

### Upload a file

Uploads a file to an existing bucket.

**Required policy permissions:** `buckets: none`; `objects: insert`

```csharp
var imagePath = Path.Combine("Assets", "fancy-avatar.png");
await supabase.Storage
    .From("avatars")
    .Upload(
        imagePath,
        "fancy-avatar.png",
        new FileOptions { CacheControl = "3600", Upsert = false });
```

### Replace an existing file

Replaces an existing file at the specified path with a new one.

**Required policy permissions:** `buckets: none`; `objects: update and select`

```csharp
var imagePath = Path.Combine("Assets", "fancy-avatar.png");
await supabase.Storage.From("avatars").Update(imagePath, "fancy-avatar.png");
```

### Move an existing file

Moves an existing file, optionally renaming it at the same time.

**Required policy permissions:** `buckets: none`; `objects: update and select`

```csharp
await supabase.Storage
    .From("avatars")
    .Move("public/fancy-avatar.png", "private/fancy-avatar.png");
```

### Copy an existing file

Copies an existing file to a new path in the same bucket.

**Required policy permissions:** `buckets: none`; `objects: select and insert`

```csharp
await supabase.Storage
    .From("avatars")
    .Copy("public/fancy-avatar.png", "public/fancy-avatar-copy.png");
```

### Create signed URLs

Creates signed URLs to download files without requiring a signed-in user. Each URL is valid for the specified number of seconds.

**Required policy permissions:** `buckets: none`; `objects: select`

```csharp
var url = await supabase.Storage
    .From("avatars")
    .CreateSignedUrl("public/fancy-avatar.png", 60);

var paths = new List<string>
{
    "public/fancy-avatar.png",
    "public/fancy-avatar-2.png"
};
var urls = await supabase.Storage.From("avatars").CreateSignedUrls(paths, 60);
```

### Create a signed upload URL

Creates a signed URL that can be used to upload a file without requiring a signed-in user. Pair this with `UploadToSignedUrl()`.

**Required policy permissions:** `buckets: none`; `objects: insert`

```csharp
var signedUrl = await supabase.Storage
    .From("avatars")
    .CreateUploadSignedUrl("fancy-avatar.png");
```

### Upload to a signed URL

Uploads a file to a signed URL created with `CreateUploadSignedUrl()`.

**Required policy permissions:** `buckets: none`; `objects: insert`

```csharp
var imagePath = Path.Combine("Assets", "fancy-avatar.png");
var signedUrl = await supabase.Storage
    .From("avatars")
    .CreateUploadSignedUrl("fancy-avatar.png");

await supabase.Storage
    .From("avatars")
    .UploadToSignedUrl(imagePath, signedUrl);
```

### Retrieve a public URL

Retrieves the URL for an asset in a public bucket. The bucket must be public. Set it with `UpdateBucket()` or in the Supabase dashboard by opening Storage, selecting the bucket menu, and choosing **Make public**.

**Required policy permissions:** `buckets: none`; `objects: none`

```csharp
var publicUrl = supabase.Storage
    .From("avatars")
    .GetPublicUrl("public/fancy-avatar.png");
```

### Download a file

Downloads a file.

**Required policy permissions:** `buckets: none`; `objects: select`

```csharp
var bytes = await supabase.Storage
    .From("avatars")
    .Download("public/fancy-avatar.png");
```

### Delete files in a bucket

Deletes files within the same bucket.

**Required policy permissions:** `buckets: none`; `objects: delete and select`

```csharp
await supabase.Storage
    .From("avatars")
    .Remove(new List<string> { "public/fancy-avatar.png" });
```

### List all files in a bucket

Lists all files within a bucket.

**Required policy permissions:** `buckets: none`; `objects: select`

```csharp
var objects = await supabase.Storage.From("avatars").List();
```
