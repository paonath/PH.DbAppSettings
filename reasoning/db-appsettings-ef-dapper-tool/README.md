---
title: "Comprehensive Architecture and Implementation Guide: Dual EF Core & Dapper Database-Backed Configuration Tool for .NET 10"
summary: "Technical audit, architectural design, gap analysis, and implementation roadmap for transforming PH.DbAppSettings into a high-performance .NET 10 configuration tool supporting both Entity Framework Core 10 and Dapper across SQL Server, PostgreSQL, SQLite, and MySQL."
created_at: "2026-08-21T10:34:00+02:00"
author: "Antigravity Reasoning Agent"
project: "PH.DbAppSettings"
target_framework: "net10.0"
csharp_version: "14"
---

# Dual-Engine Database Configuration Tool (EF Core & Dapper) for .NET 10

## 1. Problem Statement and Requirements Overview

Modern enterprise .NET cloud deployments require robust, secure, and dynamic configuration management. While `appsettings.json` is standard during local development, relying on plain JSON files in production environments introduces security vulnerabilities, lacks runtime hot-reloading across distributed nodes, and couples configuration updates to deployment cycles.

The objective of `PH.DbAppSettings` is to provide a enterprise-grade .NET 10 library and tooling ecosystem that:
- Reads configuration from relational databases via either **Entity Framework Core 10** or **Dapper** micro-ORM.
- Exposes settings as a native `IConfigurationProvider`, allowing standard `IConfiguration.GetSection(...)` and Microsoft Options patterns (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) to bind without custom wrapper methods.
- Completely eliminates the need for `appsettings.json` in production containers, requiring solely a database connection string provided via environment variables.
- Supports fine-grained runtime insertion, deletion, and updates of configuration entries via a clean write API (`IDbAppSettingsWriter`) and automatic seeding from bootstrap sources.
- Enables optional encryption at rest using AES-GCM 256-bit for sensitive values.
- Delivers multi-database support covering SQL Server, PostgreSQL, SQLite, and MySQL.
- Introduces `AGENTS.md` governance files to guide AI and human developers across the repository.

### Referenced Workspace Documents

- `README.md`
- `spec/README.md`
- `spec/implementation-plan.md`
- `src/PH.DbAppSettings/PH.DbAppSettings.csproj`
- `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`
- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`
- `src/PH.DbAppSettings/Data/AppSettingsDbContext.cs`
- `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`
- `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`

---

## 2. Microsoft Configuration and Options Subsystem Analysis

### 2.1 The Colon (`:`) Delimiter Contract in `Microsoft.Extensions.Configuration`

In the .NET configuration model, hierarchical paths are represented internally in `ConfigurationProvider.Data` as colon-separated keys (`Section:SubSection:Setting`).

When an application invokes:
```csharp
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));
```
`ConfigurationSection` queries all configuration providers for child keys prefixed with `"DatabaseSettings:"`. If a provider populates its internal dictionary with double underscores (e.g., `"DatabaseSettings__Host"`), `GetSection("DatabaseSettings")` cannot find child elements, resulting in unpopulated option instances.

While environment variables and database tables often use `__` for safety across shell and SQL naming rules, the `IConfigurationProvider` implementation must normalize all database keys to the standard `:` delimiter when loading them into memory.

### 2.2 Options Lifecycle and Live Reload Dynamics

.NET provides three distinct lifetimes for configuration models:

| Options Type | Lifetime | Reload Behavior | Recommended Use Case |
|---|---|---|---|
| `IOptions<T>` | Singleton | Fixed at startup | Immutable configurations, foundational services |
| `IOptionsSnapshot<T>` | Scoped | Recomputed per DI scope / HTTP request | Request-scoped services needing fresh settings |
| `IOptionsMonitor<T>` | Singleton | Real-time notifications via `IChangeToken` | Dynamic feature flags, live timeout updates |

When a background polling worker or runtime writer modifies database entries, calling `ConfigurationProvider.OnReload()` triggers the global `IConfigurationRoot` reload token. This immediately invalidates the internal cache of `IOptionsMonitor<T>`, allowing dependent services to access fresh values without application restart.

---

## 3. Current Codebase Audit and Gap Assessment

### 3.1 Existing Repository Capabilities

The current repository implements a functional SQLite prototype:
- `DbAppSettingsProvider`: Loads settings into `ConfigurationProvider.Data` from an EF Core `AppSettingsDbContext`.
- `SeedService`: Flattens bootstrap `IConfiguration` and inserts entries into `AppSettings`.
- `AesGcmValueEncryptor`: Provides authenticated 256-bit AES-GCM encryption with Base64 encoding.
- `DbAppSettingsWriter`: Performs basic upsert and delete operations using EF Core.

### 3.2 Identified Technical and Architectural Gaps

```
+-------------------------------------------------------------------------------+
| CRITICAL GAPS IN CURRENT PROTOTYPE                                            |
+-----------------------------------+-------------------------------------------+
| 1. Key Delimiter Mismatch         | Keys stored as '__' are kept as '__' in   |
|                                   | Data, breaking IOptions<T> binding.       |
+-----------------------------------+-------------------------------------------+
| 2. Zero Dapper Support            | Dapper engine, connection factories, and  |
|                                   | multi-dialect queries are absent.         |
+-----------------------------------+-------------------------------------------+
| 3. Hardcoded SQLite               | Extension methods explicitly call SQLite, |
|                                   | blocking SQL Server, Postgres, MySQL.     |
+-----------------------------------+-------------------------------------------+
| 4. Missing Timestamp Optimization | Change detection diffs all rows instead   |
|                                   | of querying MAX(UpdatedAt).               |
+-----------------------------------+-------------------------------------------+
| 5. Missing Governance Files       | No AGENTS.md files exist in repository.   |
+-----------------------------------+-------------------------------------------+
```

---

## 4. Dual-Engine Storage Architecture Design

To support both Entity Framework Core 10 and Dapper within a single unified package, the data access layer is abstracted behind `IDbAppSettingsStorageEngine`.

### 4.1 System Architecture Diagram

```mermaid
flowchart TD
    subgraph HostApp["Host Application (.NET 10)"]
        ProgramCs["Program.cs (Bootstrap)"]
        AppConfig["IConfiguration / IConfigurationRoot"]
        OptionsBind["IOptions<T> / IOptionsSnapshot<T> / IOptionsMonitor<T>"]
        AppConfig --> OptionsBind
    end

    subgraph CoreProvider["PH.DbAppSettings (Configuration Layer)"]
        Provider["DbAppSettingsProvider"]
        KeyNorm["KeyNormalizer (maps DB keys to ':')"]
        Writer["IDbAppSettingsWriter"]
        Reloader["ReloadBackgroundService (O(1) Timestamp check)"]
        Provider --> KeyNorm
        KeyNorm --> AppConfig
    end

    subgraph StorageLayer["Storage Abstraction Layer"]
        IEngine["IDbAppSettingsStorageEngine"]
        Provider --> IEngine
        Writer --> IEngine
        Reloader --> IEngine
    end

    subgraph Engines["Pluggable Storage Engines"]
        EFEngine["EfCoreStorageEngine (AppSettingsDbContext)"]
        DapperEngine["DapperStorageEngine (ISqlDialect + DbConnection)"]
        IEngine --> EFEngine
        IEngine --> DapperEngine
    end

    subgraph Databases["Supported Database Servers"]
        SqlServer["SQL Server"]
        Postgres["PostgreSQL"]
        Sqlite["SQLite"]
        MySql["MySQL"]
        EFEngine --> Databases
        DapperEngine --> Databases
    end
```

### 4.2 Storage Engine Abstraction (`IDbAppSettingsStorageEngine`)

```csharp
namespace PH.DbAppSettings.Storage;

public interface IDbAppSettingsStorageEngine
{
    Task<IReadOnlyList<AppSettingRecord>> GetAllAsync(string environment, CancellationToken ct = default);
    Task<AppSettingRecord?> GetByKeyAsync(string key, string environment, CancellationToken ct = default);
    Task UpsertAsync(AppSettingRecord entry, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<AppSettingRecord> entries, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, string environment, CancellationToken ct = default);
    Task<bool> IsEmptyAsync(string environment, CancellationToken ct = default);
    Task EnsureSchemaCreatedAsync(CancellationToken ct = default);
    Task<DateTimeOffset?> GetLastModifiedTimestampAsync(string environment, CancellationToken ct = default);
}
```

### 4.3 Database Schema and Multi-Dialect Support

The canonical database table structure includes an `UpdatedAt` timestamp column:

```sql
-- SQL Server
CREATE TABLE AppSettings (
    [Key]       NVARCHAR(512)       NOT NULL,
    Environment NVARCHAR(64)        NOT NULL DEFAULT 'Production',
    [Value]     NVARCHAR(4000)      NULL,
    IsEncrypted BIT                 NOT NULL DEFAULT 0,
    UpdatedAt   DATETIMEOFFSET      NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT PK_AppSettings PRIMARY KEY ([Key], Environment)
);

-- PostgreSQL
CREATE TABLE "AppSettings" (
    "Key"       VARCHAR(512)        NOT NULL,
    "Environment" VARCHAR(64)       NOT NULL DEFAULT 'Production',
    "Value"     TEXT                NULL,
    "IsEncrypted" BOOLEAN           NOT NULL DEFAULT FALSE,
    "UpdatedAt" TIMESTAMPTZ         NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_AppSettings" PRIMARY KEY ("Key", "Environment")
);
```

### 4.4 Ergonomic Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Bootstrap configuration reading connection string from env var
var bootstrapConfig = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

// Option A: Using Dapper Engine
builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.Environment = builder.Environment.EnvironmentName;
    options.ReloadInterval = TimeSpan.FromMinutes(5);
    options.EncryptValues = true;
    options.UseDapper(dapper => dapper.UseSqlServer(connectionString));
});

// Option B: Using Entity Framework Core Engine
// builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
// {
//     options.UseEntityFramework(ef => ef.UseNpgsql(connectionString));
// });

builder.Services.AddDbAppSettingsServices(options);
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
```

---

## 5. Phased Implementation Roadmap

### Phase 1: Storage Abstractions and Key Normalization
- Implement `KeyNormalizer` with bidirectional transformation between `:` and `__`.
- Define `AppSettingRecord` and `IDbAppSettingsStorageEngine`.
- Update `DbAppSettingsOptions` and `DbAppSettingsMutableOptions` with engine selection builders.

### Phase 2: Dapper Engine and Multi-Dialect SQL Generators
- Add `Dapper` package to `src/PH.DbAppSettings/PH.DbAppSettings.csproj`.
- Implement `ISqlDialect` and concrete dialect classes for SQL Server, PostgreSQL, SQLite, MySQL.
- Implement `DapperStorageEngine` with parameterized queries and idempotent DDL initialization.
- Write unit tests for all dialects and Dapper storage operations on SQLite.

### Phase 3: EF Core Engine Modernization
- Implement `EfCoreStorageEngine` wrapping `AppSettingsDbContext`.
- Update `AppSettingEntry` and `AppSettingEntryConfiguration` with `UpdatedAt`.
- Make `DbContextOptionsBuilder` fully configurable for all EF Core relational providers.

### Phase 4: Provider Refactoring and Options Binding
- Refactor `DbAppSettingsProvider` to consume `IDbAppSettingsStorageEngine`.
- Ensure all `Data` keys are populated with `:` delimiter.
- Refactor `ReloadBackgroundService` to use `GetLastModifiedTimestampAsync` for `O(1)` change detection.
- Refactor `DbAppSettingsWriter` to delegate to `IDbAppSettingsStorageEngine` and trigger local reload.
- Refactor `SeedService` for dual-engine batch operations.

### Phase 5: Fluent Extensions and DI Registration
- Implement fluent `UseEntityFramework` and `UseDapper` extensions.
- Modernize `AddDbAppSettingsServices` to register appropriate storage singletons and scoped writers.

### Phase 6: Verification and Test Suite
- Add unit tests verifying native `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` binding.
- Add integration tests verifying bootstrap, seed, runtime update, and reload across engines.

### Phase 7: Documentation and AGENTS.md Governance
- Create exportable SQL DDL scripts for external DBA deployments.
- Create root, library, and test suite `AGENTS.md` files.
- Update root `README.md`.

---

## 6. Repository Governance: AGENTS.md Instructions

To govern AI and developer contributions, three `AGENTS.md` files are specified:

1. **Root `AGENTS.md`**: Enforces .NET 10, C# 14, dual-engine abstraction rules, `ILogger<T>` structured logging, security guidelines (no committed credentials), and build/test commands.
2. **Library `src/PH.DbAppSettings/AGENTS.md`**: Enforces strict separation between `Configuration/`, `Storage/`, `Data/`, `Encryption/`, and `Services/`, mandating that all data operations use `IDbAppSettingsStorageEngine`.
3. **Test Suite `tests/PH.DbAppSettings.Tests/AGENTS.md`**: Specifies xUnit testing conventions, dialect coverage requirements, and in-memory test isolation.

---

## 7. Post-Flow Workspace Modifications Summary

The following explicit file changes and additions are required in the workspace to complete the implementation:

### New Files to Create

- `src/PH.DbAppSettings/Storage/IDbAppSettingsStorageEngine.cs`
- `src/PH.DbAppSettings/Storage/AppSettingRecord.cs`
- `src/PH.DbAppSettings/Storage/ISqlDialect.cs`
- `src/PH.DbAppSettings/Storage/Dialects/SqlServerDialect.cs`
- `src/PH.DbAppSettings/Storage/Dialects/PostgreSqlDialect.cs`
- `src/PH.DbAppSettings/Storage/Dialects/SqliteDialect.cs`
- `src/PH.DbAppSettings/Storage/Dialects/MySqlDialect.cs`
- `src/PH.DbAppSettings/Storage/DapperStorageEngine.cs`
- `src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs`
- `src/PH.DbAppSettings/Configuration/KeyNormalizer.cs`
- `AGENTS.md`
- `src/PH.DbAppSettings/AGENTS.md`
- `tests/PH.DbAppSettings.Tests/AGENTS.md`
- `scripts/ddl/sqlserver_create_table.sql`
- `scripts/ddl/postgres_create_table.sql`
- `scripts/ddl/sqlite_create_table.sql`
- `scripts/ddl/mysql_create_table.sql`

### Existing Files to Modify

- `src/PH.DbAppSettings/PH.DbAppSettings.csproj`: Add `PackageReference` for `Dapper`.
- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`: Inject `IDbAppSettingsStorageEngine` and populate `Data` with `:` normalized keys.
- `src/PH.DbAppSettings/DbAppSettingsOptions.cs` and `DbAppSettingsMutableOptions.cs`: Add engine builder options and SQL dialect properties.
- `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`: Add `UseEntityFramework` and `UseDapper` extension methods.
- `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`: Delegate storage operations to `IDbAppSettingsStorageEngine`.
- `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`: Optimize change detection using timestamp checks.
- `src/PH.DbAppSettings/Data/AppSettingEntry.cs` and `AppSettingEntryConfiguration.cs`: Add `UpdatedAt` property and column configuration.
- `README.md`: Update documentation with dual-engine configuration patterns.
