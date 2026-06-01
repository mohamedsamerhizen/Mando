# Deployment and Local Operations

This project is an ASP.NET Core Web API backed by SQL Server. Production-like runs must provide secrets through environment variables, user-secrets, or a secret manager. Do not commit real JWT keys, database passwords, or seed account passwords.

## Local Development

1. Install .NET 9 SDK and SQL Server LocalDB or SQL Server Developer Edition.
2. Configure secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-random-characters" --project Mando.Api
dotnet user-secrets set "SeedAdmin:Password" "replace-with-a-local-password" --project Mando.Api
dotnet user-secrets set "SeedManager:Password" "replace-with-a-local-password" --project Mando.Api
dotnet user-secrets set "SeedSalesReps:0:Password" "replace-with-a-local-password" --project Mando.Api
```

3. Apply migrations:

```powershell
dotnet ef database update --project Mando.Api
```

4. Run the API:

```powershell
dotnet run --project Mando.Api
```

Swagger is enabled only in Development.

## Docker Compose

Docker Compose files are included for reviewer convenience. Copy `.env.example` to `.env`, replace every `REPLACE_WITH...` value, then run:

```powershell
docker compose up --build
```

The API listens on `http://localhost:8080`. SQL Server is exposed on host port `14333`.

The compose file enables startup migrations and seed users only in Development. This is useful for local portfolio review, but do not use automatic migrations/seeding in production.

## Configuration

Required settings:

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server database connection |
| `Jwt:Key` | HMAC signing key, at least 32 characters |
| `Jwt:Issuer` | Expected token issuer |
| `Jwt:Audience` | Expected token audience |
| `Jwt:ExpiryMinutes` | Access token lifetime |
| `SeedAdmin:*`, `SeedManager:*`, `SeedSalesReps:*` | Development/testing seed accounts when seeding is enabled |

Operational settings:

| Key | Purpose |
| --- | --- |
| `Startup:ApplyMigrationsOnStartup` | Applies EF migrations on startup only when allowed |
| `Startup:RunSeedOnStartup` | Runs seed users/data on startup only when allowed |
| `Gps:*` | Visit start/end distance and accuracy thresholds |
| `ForwardedHeaders:*` | Reverse-proxy forwarding configuration |
| `RateLimiting:Login:*` | Login fixed-window permit limit and window |
| `RateLimiting:SensitiveMutation:*` | Fixed-window throttling for payment review and visit lifecycle mutations |

Docker Compose also accepts `RATE_LIMIT_LOGIN_PERMIT_LIMIT`, `RATE_LIMIT_LOGIN_WINDOW_SECONDS`, `RATE_LIMIT_SENSITIVE_MUTATION_PERMIT_LIMIT`, and `RATE_LIMIT_SENSITIVE_MUTATION_WINDOW_SECONDS` from `.env`.

## Verification

Before opening a pull request or commit:

```powershell
dotnet restore Mando.sln
dotnet build Mando.sln
dotnet test Mando.sln
dotnet format Mando.sln --verify-no-changes
```

CI runs the same restore, build, test, and format checks on GitHub Actions.
