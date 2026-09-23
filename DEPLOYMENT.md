# UPTSLK deployment guide

This guide deploys the UPTSLK demonstration environment with:

- **API:** ASP.NET Core 8 on [WSO2 Developer Platform (Choreo)](https://console.choreo.dev)
- **Web app:** React/Vite on [Vercel](https://vercel.com)
- **Database:** managed PostgreSQL on [Neon](https://neon.tech)
- **Source control and builds:** GitHub

## Deployment architecture

```text
Browser
  |
  v
Vercel React application
  |  VITE_API_BASE_URL
  v
Choreo ASP.NET Core API
  |  ConnectionStrings__DefaultConnection
  v
Neon PostgreSQL
```

The database URL is stored only in Choreo. The browser never receives it.

## Before starting

You need:

- A GitHub repository containing this project.
- A Neon account and a new Neon project.
- A Choreo account.
- A Vercel account.
- .NET 8, Node.js and pnpm locally, so that you can validate the build before
  deploying.

Run these checks from the repository root before pushing:

```bash
cd apps/api && dotnet build
cd ../web && pnpm install && pnpm lint && pnpm build
```

In Windows PowerShell:

```powershell
Set-Location apps/api; dotnet build
Set-Location ../web; pnpm install; pnpm lint; pnpm build
```

Do not commit `.env` files, connection strings, JWT keys, Stripe keys, or
administrator passwords.

## 1. Create the Neon database

1. In Neon, create a project, preferably in a region near the Choreo API
   deployment region.
2. Use the default branch for the viva, or rename/create one such as
   `viva-production`.
3. Open **Connect** and copy the PostgreSQL connection string.
4. Keep the full string private. It normally includes `sslmode=require`; do
   not remove it.

There is no need to create tables manually or import a SQL schema. UPTSLK
contains EF Core migrations under `apps/api/Migrations`. On the first API
startup, EF Core creates the schema; on later deployments it applies only the
migrations that are missing from the `__EFMigrationsHistory` table.

For a small demo, the pooled Neon URL is suitable for normal API traffic. If a
future migration tool needs session-specific PostgreSQL behaviour, use Neon's
direct connection URL for that one-off job.

## 2. Generate deployment secrets

Create a new JWT signing key and a strong bootstrap administrator password.
Do this once per deployed environment; do not reuse a local-development key.

### Linux

```bash
openssl rand -base64 64
openssl rand -base64 24
```

The first output is the JWT key. The second can be used as a password, though
you may prefer to set a memorable strong password for the viva administrator.

### Windows PowerShell

```powershell
$jwtBytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
[Convert]::ToBase64String($jwtBytes)

$passwordBytes = New-Object byte[] 24
[System.Security.Cryptography.RandomNumberGenerator]::Fill($passwordBytes)
[Convert]::ToBase64String($passwordBytes)
```

Store these values in a password manager until they have been entered as
Choreo secrets. If any real value was committed or pasted into a public place,
rotate it before deploying.

## 3. Prepare the API for cross-site authentication

This is required before using Vercel and Choreo together.

The web app calls the API with `credentials: "include"`. The current API
cookie settings in `AuthController.SetAuthCookies` are designed for local
development (`Secure = false`, `SameSite = Lax`). Browsers will not reliably
send those cookies from a Vercel site to a separate Choreo domain.

Before the cloud deployment, update the API to issue production cookies with:

```csharp
Secure = true,
SameSite = SameSiteMode.None
```

Keep the existing local values for local HTTP development, ideally by making
the cookie policy environment-based. This change must be implemented and
tested before the final demo; otherwise login and refresh-session behaviour
can fail even though the API itself is online.

The API's CORS policy already reads `Payments:Stripe:WebAppBaseUrl`, so the
same configuration value should be set to the final Vercel URL.

## 4. Deploy the API to Choreo

1. Push the current branch, including all committed EF migrations, to GitHub.
   A public repository may be required by the Choreo free tier.
2. Go to [Choreo](https://console.choreo.dev), create a project, then create a
   **Service** component from the GitHub repository.
3. Set the component directory to `apps/api`.
4. Choose the **.NET** build preset and **.NET 8** runtime.
5. Configure the service port as **8080**.
6. In the deployment configuration, add the variables below. Mark every
   sensitive value as a secret.
7. Disable Choreo endpoint OAuth2 authentication. UPTSLK already authenticates
   users with its own JWT and cookies.
8. Deploy to the Development environment. Copy the generated public API URL.

The API normally listens on port 5250 locally. This Choreo setting overrides
it and makes the application listen on port 8080:

| Choreo name | Value | Secret? |
| --- | --- | --- |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` | No |
| `ConnectionStrings__DefaultConnection` | Neon PostgreSQL connection string | Yes |
| `Jwt__Key` | Generated 64-byte Base64 value | Yes |
| `Jwt__Issuer` | `upts-api` | No |
| `Jwt__Audience` | `upts-web` | No |
| `BootstrapAdmin__Email` | Your administrator email | No |
| `BootstrapAdmin__Password` | Strong administrator password | Yes |
| `Payments__Stripe__WebAppBaseUrl` | Final Vercel URL, for example `https://uptslk-demo.vercel.app` | No |
| `Payments__Stripe__SecretKey` | Stripe secret key, if demonstrating Stripe payments | Yes |
| `Payments__Stripe__WebhookSecret` | Stripe webhook signing secret, if using webhooks | Yes |

The double underscores are intentional: ASP.NET Core maps them to nested
configuration keys, for example
`ConnectionStrings__DefaultConnection` becomes
`ConnectionStrings:DefaultConnection`.

### First Choreo deployment

On the first successful start, the API automatically:

1. Connects to Neon.
2. Creates all tables, indexes and foreign keys from the committed EF Core
   migrations.
3. Records them in `__EFMigrationsHistory`.
4. Creates the bootstrap administrator account if that email does not already
   exist.

You do **not** run `dotnet ef database update` manually against Neon and do
not manually create tables in the Neon SQL editor.

### Verify the API

This API does not currently expose a dedicated `/health` endpoint. After the
deployment becomes active, verify its public read endpoint instead:

```bash
curl --fail https://YOUR_CHOREO_API_URL/api/v1/centres
```

In Windows PowerShell:

```powershell
Invoke-RestMethod https://YOUR_CHOREO_API_URL/api/v1/centres
```

An empty JSON array is a successful result on a new database. Check the Choreo
runtime logs if startup fails; a database or migration problem will be visible
there.

## 5. Add the viva data

After the API and schema are live, sign in with the bootstrap administrator
account and add the demo records through the UI or Swagger (if you have
explicitly enabled it for the demo): centres, bays, routes, directions,
schedules, vehicles, drivers, fare rules, trips and test accounts.

This is operational demo data, not schema data, so EF migrations do not create
it automatically. Once entered, it remains in Neon across Choreo/Vercel
redeployments unless you delete the Neon branch/database.

## 6. Deploy the web application to Vercel

1. In Vercel, select **Add New → Project** and import the same GitHub
   repository.
2. Set the root directory to `apps/web`.
3. Vercel should detect Vite. Use the default build command (`pnpm build`) and
   output directory (`dist`).
4. Add the environment variable below for Production. Add it to Preview too if
   you intend to test preview deployments.
5. Deploy, then copy the final Vercel URL.

| Vercel variable | Value |
| --- | --- |
| `VITE_API_BASE_URL` | Choreo API base URL, for example `https://YOUR_CHOREO_API_URL` |

Do not include a trailing slash. The web client appends paths such as
`/api/v1/centres` itself.

`VITE_` variables are embedded into the browser bundle. Never put a database
connection string, JWT signing key, Stripe secret key, or administrator
password in Vercel environment variables.

## 7. Complete the CORS loop

After Vercel gives you the final URL:

1. Go back to Choreo.
2. Set `Payments__Stripe__WebAppBaseUrl` to that exact URL, such as
   `https://uptslk-demo.vercel.app`.
3. Redeploy the API so its CORS allow-list includes the final frontend origin.
4. Redeploy Vercel if `VITE_API_BASE_URL` changed.

Then open the Vercel site, sign in, refresh the page, and verify that API data
loads. Use the browser Network tab to diagnose CORS or cookie errors.

## 8. Future database changes

For each schema change, use this process locally:

```bash
cd apps/api
dotnet ef migrations add DescriptiveMigrationName
dotnet build
```

Review the generated migration and model snapshot, commit both, then deploy
the API. The current startup migration code applies the new migration to Neon
once. It will not repeat migrations already recorded in
`__EFMigrationsHistory`.

For this viva, do not add a GitHub Action solely to run migrations. It would
require duplicating the Neon secret in another system and adds another moving
part. The existing single-replica startup migration is the simplest approach.

For a real multi-replica production deployment, move migrations out of API
startup and run an EF Core migration bundle once as a separate deployment job.
This prevents two API instances from attempting a schema update at the same
time. See the [EF Core migration deployment guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli).

## Useful references

- [Choreo CI/CD and deployment configuration](https://docs.dv.choreo.dev/choreo/docs/choreo-concepts/ci-cd/)
- [Neon database branching workflow](https://neon.com/docs/get-started-with-neon/workflow-primer)
- [EF Core applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli)
