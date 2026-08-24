---
step: "005"
title: "Gap Analysis and Breaking Changes Catalog"
status: "completed"
created_at: "2026-08-24T10:13:35+02:00"
---

# Gap Analysis and Breaking Changes Catalog

## Purpose

Catalog all architectural and code gaps between the existing concrete SQLite implementation and the target abstract multi-provider architecture.
Document all breaking changes and migration paths for consumers, tests, and example applications.

## Detailed Gap Analysis

### 1. Context Hierarchy & Construction

- **Current State**: `AppSettingsDbContext` is a concrete class accepting `DbContextOptions<AppSettingsDbContext>`.
- **Target State**: `AppSettingsDbContext` is a `public abstract class AppSettingsDbContext : DbContext` with a generic variant `public abstract class AppSettingsDbContext<TContext> : AppSettingsDbContext where TContext : DbContext`.
- **Breaking Change**: Direct instantiation via `new AppSettingsDbContext(options)` is no longer possible.
- **Migration Path**: Consumers define `public class AppDbContext(DbContextOptions<AppDbContext> options) : AppSettingsDbContext<AppDbContext>(options)` in their application project.

### 2. Design-Time Factory & Migrations

- **Current State**: `AppSettingsDbContextFactory` implements `IDesignTimeDbContextFactory<AppSettingsDbContext>` hardcoded to SQLite and contains concrete migration snapshots in `src/PH.DbAppSettings/Data/Migrations/`.
- **Target State**: Concrete SQLite factory is replaced by `public abstract class AppSettingsDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : AppSettingsDbContext`. Concrete migration snapshots in the core library are removed.
- **Breaking Change**: `dotnet ef migrations` commands must target the host application's DbContext.
- **Migration Path**: Host applications create their own migrations using their target provider (e.g. `Npgsql`, `SqlServer`, `Pomelo.MySql`, `Sqlite`).

### 3. Storage Engine Schema Creation & Provider Support

- **Current State**: `EfCoreStorageEngine.EnsureSchemaCreatedAsync` only executes `Database.EnsureCreatedAsync()`.
- **Target State**: `EfCoreStorageEngine.EnsureSchemaCreatedAsync` checks `UseMigrations` option. If `true`, it calls `Database.MigrateAsync(ct)`; otherwise, it calls `Database.EnsureCreatedAsync(ct)`.
- **Breaking Change**: None (additive property `UseMigrations` defaults to `false`).

### 4. Configuration & DI Extension APIs

- **Current State**: `DbAppSettingsExtensions.AddDbAppSettingsServices` registers `services.AddDbContext<AppSettingsDbContext>` with hardcoded SQLite. `DbAppSettingsMutableOptions.UseEntityFrameworkSqlite` hardcodes SQLite.
- **Target State**: Generic extensions `AddDbAppSettings<TContext>` and `AddDbAppSettingsServices<TContext>`. `UseEntityFramework<TContext>` accepts configuration delegates for any provider (PostgreSQL, SQL Server, MySQL, SQLite).
- **Breaking Change**: Non-generic `AddDbAppSettingsServices` without engine configuration will require specifying the context type or explicit storage engine.

### 5. Provider & Reload Background Service Fallbacks

- **Current State**: `DbAppSettingsProvider.CreateDefaultEfEngine()` and `ReloadBackgroundService` fallback constructor silently construct a SQLite context if no engine is provided.
- **Target State**: Silent SQLite fallbacks removed. An explicit exception is thrown if no storage engine is configured.
- **Breaking Change**: Calling `ReloadBackgroundService` without passing a configured `IDbAppSettingsStorageEngine` is removed.

### 6. Test Suite and Example Project Coupling

- **Current State**: Tests and sample Minimal API directly reference concrete `AppSettingsDbContext`.
- **Target State**: Tests use `TestAppSettingsDbContext : AppSettingsDbContext<TestAppSettingsDbContext>`. Example Minimal API uses `AppDbContext : AppSettingsDbContext<AppDbContext>`.

## Summary Table

| Component | Current State | Target State | Impact |
| :--- | :--- | :--- | :--- |
| `AppSettingsDbContext` | Concrete class | `public abstract` class + generic base | Breaking |
| `AppSettingsDbContextFactory` | Concrete SQLite factory | Abstract base factory `AppSettingsDesignTimeDbContextFactory<T>` | Breaking |
| `Core Migrations` | Snapshot in core library | Owned by host application | Breaking |
| `EfCoreStorageEngine` | `EnsureCreatedAsync` only | Supports `EnsureCreatedAsync` and `MigrateAsync` | Non-breaking |
| `DbAppSettingsExtensions` | Non-generic SQLite DI | Generic `AddDbAppSettings<TContext>` | Breaking (Major upgrade) |
| `DbAppSettingsProvider` | Silent SQLite fallback | Explicit error if engine missing | Non-breaking / Safe |
| `ReloadBackgroundService` | SQLite fallback constructor | Requires injected storage engine | Breaking |

## Handoff

- Findings: Comprehensive gap catalog compiled with clear migration paths for host applications and internal test harnesses.
- Confidence: high.
- Assumptions: All breaking changes will be validated through unit and integration tests.
- Open questions: None.
