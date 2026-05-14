# PH.DbAppSettings

A .NET 10 library that reads configuration from `appsettings.json`, persists all key/value pairs to a database via Entity Framework Core, and exposes them as a native `IConfigurationProvider` — replacing `appsettings.json` in production.

## Why

In production environments, `appsettings.json` is a risk vector: it contains secrets in plain text, is not securely versioned, and does not support hot updates without redeployment. Centralizing configuration in a database enables:

- Updating settings without redeployment
- Separation of code and configuration
- Optional encryption of sensitive values at rest

## Installation

```xml
<PackageReference Include="PH.DbAppSettings" Version="1.0.0" />
```

## Minimum Required Configuration

The **only** configuration that must remain outside the database is the connection string to the configuration database itself.

```json
// appsettings.json — only the connection string is needed
{
  "DbAppSettings": {
    "ConnectionString": "Data Source=appconfig.db"
  }
}
```

In production, use **exclusively** the environment variable — no `appsettings.json` needed:

```bash
export DbAppSettings__ConnectionString="Server=...;Database=AppConfig;..."
```

## Usage in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Bootstrap config: reads only the connection string
var bootstrapConfig = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

// 2. Add DbAppSettings as the main configuration provider
builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.AutoMigrate = true;
    options.SeedOnEmpty = true;
    options.ExcludeKeysFromSeed = ["DbAppSettings__ConnectionString"];
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});

// 3. Register DI services (writer, seed service, background reload)
builder.Services.AddDbAppSettingsServices(/* your options */);

// 4. From here on, IConfiguration reads from DB
builder.Services.Configure<MyOptions>(builder.Configuration.GetSection("MyApp"));
```

## Options Reference (`DbAppSettingsMutableOptions`)

| Property | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | **required** | Connection string to the configuration DB |
| `Environment` | `string` | `"Production"` | Environment name (matches `ASPNETCORE_ENVIRONMENT`) |
| `AutoMigrate` | `bool` | `true` | Automatically apply EF migrations on startup |
| `SeedOnEmpty` | `bool` | `true` | Seed from `appsettings.json` if DB is empty |
| `ForceReseed` | `bool` | `false` | Force re-seed, overwriting existing DB values |
| `ExcludeKeysFromSeed` | `IReadOnlyList<string>` | `[]` | Keys to exclude from seeding |
| `EncryptValues` | `bool` | `false` | Enable AES-GCM encryption of values at rest |
| `ReloadInterval` | `TimeSpan?` | `null` | Auto-reload interval (null = disabled) |
| `SchemaName` | `string` | `"dbo"` | DB schema name |
| `TableName` | `string` | `"AppSettings"` | DB table name |

## Key Format

Configuration keys follow the same notation as .NET environment variables, using double underscore `__` as the hierarchy separator:

| `appsettings.json` path | DB key |
|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` |
| `MyApp:FeatureFlags:EnableCache` | `MyApp__FeatureFlags__EnableCache` |
| `AllowedHosts:0` | `AllowedHosts__0` |

## Database Schema

```sql
CREATE TABLE AppSettings (
    [Key]       NVARCHAR(512)   NOT NULL,
    Environment NVARCHAR(64)    NOT NULL DEFAULT 'Production',
    [Value]     NVARCHAR(4000)  NULL,
    IsEncrypted BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_AppSettings PRIMARY KEY ([Key], Environment)
);
```

## Usage Scenarios

### Scenario A — First boot in production

1. Operator sets `DbAppSettings__ConnectionString` as an env var in the container.
2. Application starts, EF applies migrations, `AppSettings` table is created.
3. DB is empty → `SeedService` reads `appsettings.json` (if present) and current env vars.
4. All keys are saved to DB (excluding the bootstrap connection string).
5. `DbAppSettingsProvider` loads from DB and populates `IConfiguration`.
6. Application is operational. `appsettings.json` can be removed from the Docker image.

### Scenario B — Update a value without redeployment

1. An operator updates a row directly in the DB (or via a panel using `IDbAppSettingsWriter`).
2. When `ReloadInterval` expires, `ReloadBackgroundService` detects the change.
3. `provider.Load()` reloads values; `IOptionsMonitor<T>` notifies consumers.
4. No redeployment needed.

### Scenario C — Local development

1. Developer keeps `appsettings.Development.json` with all values.
2. `SeedOnEmpty = true` populates the local DB (SQLite) on first startup.
3. The flow is identical to production, ensuring behavioral parity.

## Runtime Write API

```csharp
public interface IDbAppSettingsWriter
{
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
```

Inject `IDbAppSettingsWriter` to update or delete configuration values at runtime.

## Encryption

When `EncryptValues = true`, values are encrypted using AES-GCM 256-bit before storage. Set the encryption secret via environment variable:

```bash
export DbAppSettings__EncryptionSecret="your-strong-secret"
```

## Security

| Aspect | Solution |
|---|---|
| Connection string in production | Only via env var, never in files |
| Sensitive values in DB | `IsEncrypted` column, AES-GCM encryption via `IValueEncryptor` |
| DB access | DB user with minimal permissions (`SELECT`, `INSERT`, `UPDATE` on `AppSettings` only) |
| Injection | All access via EF with parameterized queries |

## Project Structure

```
PH.DbAppSettings/
├── src/
│   └── PH.DbAppSettings/
│       ├── Configuration/
│       │   ├── DbAppSettingsConfigurationSource.cs
│       │   └── DbAppSettingsProvider.cs
│       ├── Data/
│       │   ├── AppSettingEntry.cs
│       │   ├── AppSettingEntryConfiguration.cs
│       │   ├── AppSettingsDbContext.cs
│       │   ├── AppSettingsDbContextFactory.cs
│       │   └── Migrations/
│       ├── Encryption/
│       │   ├── IValueEncryptor.cs
│       │   └── AesGcmValueEncryptor.cs
│       ├── Services/
│       │   ├── IDbAppSettingsWriter.cs
│       │   ├── DbAppSettingsWriter.cs
│       │   ├── SeedService.cs
│       │   └── ReloadBackgroundService.cs
│       ├── DbAppSettingsOptions.cs
│       ├── DbAppSettingsMutableOptions.cs
│       └── DbAppSettingsExtensions.cs
└── tests/
    └── PH.DbAppSettings.Tests/
        ├── EncryptionTests.cs
        ├── KeyNormalizationTests.cs
        ├── SeedServiceTests.cs
        ├── DbAppSettingsProviderTests.cs
        └── IntegrationTests/
            └── BootstrapIntegrationTests.cs
```
