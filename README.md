# PH.DbAppSettings

A high-performance .NET 10 library that persists configuration settings in relational databases using either **Entity Framework Core 10** or **Dapper**, exposing them as a native `IConfigurationProvider` to seamlessly replace `appsettings.json` in production with full support for `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>`.

## Key Features

- **Dual Engine Architecture**: Choose between **Entity Framework Core 10** and ultra-fast **Dapper** micro-ORM in the same library.
- **Multi-Dialect Database Support**: Works natively with **SQL Server**, **PostgreSQL**, **SQLite**, and **MySQL / MariaDB**.
- **Native Options Binding**: Normalized `:` keys in memory ensure standard `services.Configure<TOptions>(config.GetSection("..."))` and `IOptionsMonitor<T>` work without friction.
- **$O(1)$ Timestamp Reload**: Remote change detection queries `MAX(UpdatedAt)` instead of expensive full table scans.
- **CLI Tool (`dbappsettings`)**: Standalone CLI to analyze `appsettings.json`, detect sensitive credentials, import into SQL tables, and export back to structured JSON.
- **At-Rest Encryption**: Optional AES-GCM 256-bit authenticated encryption for sensitive configuration values.

---

## Installation

```bash
# Core Library
dotnet add package PH.DbAppSettings

# Global CLI Tool
dotnet tool install --global PH.DbAppSettings.Cli
```

---

## Quick Start (Program.cs)

### 1. Using Dapper (Recommended for lightweight, high-performance setups)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Bootstrap config (reads DB connection string from env var or bootstrap appsettings.json)
var bootstrapConfig = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

// Add DbAppSettings provider with Dapper engine
builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.UseDapperSqlite("Data Source=appconfig.db");
    // Or for SQL Server / PostgreSQL / MySQL:
    // options.UseDapper(() => new SqlConnection(connString), new SqlServerDialect());
    
    options.AutoMigrate = true;
    options.SeedOnEmpty = true;
    options.ReloadInterval = TimeSpan.FromSeconds(30);
});

// Register DI services
builder.Services.AddDbAppSettingsServices(options =>
{
    options.UseDapperSqlite("Data Source=appconfig.db");
    options.ReloadInterval = TimeSpan.FromSeconds(30);
});

// Standard Microsoft Options binding works out-of-the-box!
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("Features"));
```

### 2. Using Entity Framework Core

```csharp
builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.UseEntityFrameworkSqlite("Data Source=appconfig.db");
    // Or with custom DbContext options:
    // options.UseEntityFramework(b => b.UseSqlServer(connString));
    
    options.AutoMigrate = true;
    options.SeedOnEmpty = true;
    options.ReloadInterval = TimeSpan.FromMinutes(1);
});
```

---

## CLI Tool Usage (`dbappsettings`)

The CLI tool allows inspecting, importing, and exporting `appsettings.json` files directly from the command line:

### 1. Analyze `appsettings.json`

Analyzes local JSON configuration files, displays flattened key paths, value types, and flags sensitive properties (passwords, connection strings, secrets, tokens):

```bash
dbappsettings analyze appsettings.json --detailed
```

### 2. Import into Database

Imports all flattened settings from an `appsettings.json` file into the target database table with automatic schema creation:

```bash
# SQLite
dbappsettings import appsettings.json -c "Data Source=appconfig.db" -d sqlite -e Production

# SQL Server with encryption for sensitive keys
dbappsettings import appsettings.json -c "Server=localhost;Database=ConfigDb;User Id=sa;Password=secret;" -d sqlserver -e Production --encrypt-secret "my-32-char-secret-key-12345678"

# PostgreSQL
dbappsettings import appsettings.json -c "Host=localhost;Database=configdb;Username=postgres;Password=secret" -d postgres -e Production
```

### 3. Export Database to JSON

Queries the database configuration table for a specific environment and reconstructs structured, indented JSON:

```bash
dbappsettings export -c "Data Source=appconfig.db" -d sqlite -e Production -o appsettings.Production.json
```

---

## Options Reference (`DbAppSettingsMutableOptions`)

| Property / Method | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | `null` | Connection string to the configuration database |
| `Environment` | `string` | `"Production"` | Environment name (matches `ASPNETCORE_ENVIRONMENT`) |
| `UseDapper(...)` | `method` | - | Configures Dapper engine with connection factory & SQL dialect |
| `UseDapperSqlite(...)` | `method` | - | Configures Dapper engine for SQLite |
| `UseEntityFramework(...)` | `method` | - | Configures Entity Framework Core engine |
| `UseEntityFrameworkSqlite(...)`| `method` | - | Configures Entity Framework Core engine for SQLite |
| `AutoMigrate` | `bool` | `true` | Creates table schema automatically on startup if missing |
| `SeedOnEmpty` | `bool` | `true` | Seeds initial settings from bootstrap config if table is empty |
| `ForceReseed` | `bool` | `false` | Overwrites existing database entries during seed |
| `ExcludeKeysFromSeed` | `IReadOnlyList<string>` | `[]` | List of keys to exclude from initial database seeding |
| `EncryptValues` | `bool` | `false` | Enables AES-GCM 256-bit value encryption |
| `ReloadInterval` | `TimeSpan?` | `null` | Polling interval for $O(1)$ timestamp change detection |
| `SchemaName` | `string` | `"dbo"` | Schema name for the configuration table |
| `TableName` | `string` | `"AppSettings"` | Name of the configuration table |

---

## Database Schema

```sql
CREATE TABLE AppSettings (
    [Key]          NVARCHAR(512)      NOT NULL,
    [Environment]  NVARCHAR(64)       NOT NULL DEFAULT 'Production',
    [Value]        NVARCHAR(MAX)      NULL,
    [IsEncrypted]  BIT                NOT NULL DEFAULT 0,
    [UpdatedAt]    DATETIMEOFFSET     NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT PK_AppSettings PRIMARY KEY ([Key], [Environment])
);
```

---

## Runtime Writer API

Inject `IDbAppSettingsWriter` to dynamically update or delete configuration entries at runtime:

```csharp
public interface IDbAppSettingsWriter
{
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
```

Updating a key updates `UpdatedAt` immediately, prompting other nodes to reload automatically when their `ReloadInterval` elapses.

---

## Project Structure

```
PH.DbAppSettings/
├── src/
│   ├── PH.DbAppSettings/              # Core library
│   │   ├── Configuration/             # Provider, source, KeyNormalizer
│   │   ├── Storage/                   # Storage engine abstractions, Dapper, EF Core, dialects
│   │   │   └── Dialects/              # SqlServer, PostgreSql, Sqlite, MySql dialects
│   │   ├── Data/                      # AppSettingsDbContext, AppSettingEntry
│   │   ├── Encryption/                # IValueEncryptor, AesGcmValueEncryptor
│   │   ├── Services/                  # Writer, SeedService, ReloadBackgroundService
│   │   ├── DbAppSettingsOptions.cs
│   │   ├── DbAppSettingsMutableOptions.cs
│   │   └── DbAppSettingsExtensions.cs
│   └── PH.DbAppSettings.Cli/          # Global CLI tool (dbappsettings)
│       ├── Commands/                  # AnalyzeCommand, ImportCommand, ExportCommand
│       ├── Models/                    # FlattenedSettingItem, AppSettingsAnalysisResult
│       ├── Services/                  # AppSettingsJsonAnalyzer, StorageEngineFactory
│       └── Program.cs
├── tests/
│   └── PH.DbAppSettings.Tests/        # 84 unit & integration tests (100% green)
│       ├── KeyNormalizerTests.cs
│       ├── NativeOptionsBindingTests.cs
│       ├── SqlDialectTests.cs
│       ├── DapperStorageEngineTests.cs
│       ├── EfCoreStorageEngineTests.cs
│       ├── TimestampReloadTests.cs
│       ├── AppSettingsJsonAnalyzerTests.cs
│       └── CliCommandTests.cs
├── specs/                             # TDD specifications
├── reasoning/                         # Analysis and reasoning trajectory
└── AGENTS.md                          # Repository governance instructions
```
