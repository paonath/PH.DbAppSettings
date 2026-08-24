---
title: "Step 003: Dapper and EF Core Dual Storage Engine Architecture Design"
step_number: 3
author: "Orchestrator"
experts_involved: ["data-access-expert", "architecture-expert", "dotnet-config-expert"]
reasoning_path: "reasoning/db-appsettings-ef-dapper-tool"
---

# Step 003: Dapper and EF Core Dual Storage Engine Architecture Design

## Purpose

Design a decoupled, dual-engine storage architecture allowing consumers to choose between **Entity Framework Core 10** and **Dapper** micro-ORM for persisting and loading application configuration.
Define storage engine abstractions, database dialect support, schema management strategies, and DI registration patterns.

## Discovered Facts

### Dapper Characteristics and Requirements

- **Lightweight Micro-ORM**: Dapper executes fast parameterized queries on raw `System.Data.Common.DbConnection` instances without change tracking or heavy DbContext overhead.
- **Connection Lifecycle**: Dapper requires an ADO.NET `DbConnection` factory (`Func<DbConnection>` or `IDbConnectionFactory`).
- **Dialect Differences**:
  - SQL Server / Azure SQL: `[Key]`, `NVARCHAR(512)`, `BIT`, `MERGE` or `IF NOT EXISTS ... INSERT ELSE UPDATE`.
  - PostgreSQL: `"Key"`, `VARCHAR(512)`, `BOOLEAN`, `ON CONFLICT ("Key", "Environment") DO UPDATE SET ...`.
  - SQLite: `[Key]` or `"Key"`, `TEXT`, `INTEGER`, `INSERT INTO ... ON CONFLICT DO UPDATE ...`.
  - MySQL: `` `Key` ``, `VARCHAR(512)`, `TINYINT(1)`, `ON DUPLICATE KEY UPDATE ...`.

### Entity Framework Core Characteristics and Requirements

- **DbContext Lifecycle**: Requires `DbContextOptionsBuilder` configured with provider packages (`Microsoft.EntityFrameworkCore.SqlServer`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Sqlite`, `Pomelo.EntityFrameworkCore.MySql`).
- **Schema Migrations**: EF Core manages schema evolution through migrations (`__EFMigrationsHistory`) or `EnsureCreatedAsync()`.

## Architecture Specification

### 1. Storage Engine Interface (`IDbAppSettingsStorageEngine`)

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

### 2. Common Data Record (`AppSettingRecord`)

```csharp
namespace PH.DbAppSettings.Storage;

public sealed record AppSettingRecord
{
    public required string Key { get; init; }
    public string Environment { get; init; } = "Production";
    public string? Value { get; init; }
    public bool IsEncrypted { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### 3. Engine Implementation Strategies

#### A. Entity Framework Core Engine (`EfCoreStorageEngine`)

- Implements `IDbAppSettingsStorageEngine` via `AppSettingsDbContext`.
- Supports user-specified database provider via `Action<DbContextOptionsBuilder>`.
- Enables `AutoMigrate` using `Database.MigrateAsync()` or lightweight fallback to `EnsureCreatedAsync()`.

#### B. Dapper Storage Engine (`DapperStorageEngine`)

- Implements `IDbAppSettingsStorageEngine` using Dapper and `IDbConnectionFactory`.
- Uses a dialect-aware SQL generator (`ISqlDialect`) covering SQL Server, PostgreSQL, SQLite, and MySQL.
- Executes idempotent DDL table creation on startup when `AutoMigrate = true`.
- Optimizes change detection by querying `MAX(UpdatedAt)` instead of loading all key/value rows during background reload polling.

### 4. Configuration and DI Ergonomics

```csharp
// Program.cs bootstrap:
builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.Environment = "Production";
    options.ReloadInterval = TimeSpan.FromMinutes(5);
    options.EncryptValues = true;

    // Option 1: Use EF Core
    options.UseEntityFramework(ef => ef.UseSqlServer(connectionString));

    // Option 2: Use Dapper
    // options.UseDapper(dapper => dapper.UseSqlServer(connectionString));
    // options.UseDapper(dapper => dapper.UsePostgres(connectionString));
    // options.UseDapper(dapper => dapper.UseSqlite(connectionString));
});
```

## Expert Deductions

### `data-access-expert` Deductions

- **Performance Advantage of Dapper**: In high-throughput environments or microservices where startup time and memory footprint are critical, Dapper eliminates DbContext instantiation overhead while delivering sub-millisecond configuration loading.
- **Dialect Abstraction**: Introducing an `ISqlDialect` interface allows supporting all major SQL databases without coupling the library to any single ADO.NET driver.
- **Enhanced Schema with `UpdatedAt`**: Adding an `UpdatedAt` timestamp column to `AppSettings` simplifies change detection from a full-table diff to a single scalar query (`SELECT MAX(UpdatedAt)`), reducing database CPU load during background reload cycles.

### `architecture-expert` Deductions

- **Unified Surface Area**: Both engines implement the identical `IDbAppSettingsStorageEngine` interface. `DbAppSettingsProvider`, `SeedService`, `DbAppSettingsWriter`, and `ReloadBackgroundService` consume the interface without knowing whether EF Core or Dapper is active.
- **Clean Extensibility**: Consumers who already have an established EF Core setup can use `UseEntityFramework`, while consumers preferring lightweight ADO/Dapper pipelines can use `UseDapper` with zero EF Core runtime footprint.

## Handoff

- **Findings**: Designed a unified storage abstraction (`IDbAppSettingsStorageEngine`) and concrete strategies for EF Core and Dapper. Added dialect support and `UpdatedAt` timestamp for high-performance reload checks.
- **Confidence**: high.
- **Assumptions**: The library will support SQL Server, PostgreSQL, SQLite, and MySQL across both EF Core and Dapper.
- **Open questions**: Should we provide a CLI migration generator / SQL export command for teams with strict DBA deployment pipelines?
