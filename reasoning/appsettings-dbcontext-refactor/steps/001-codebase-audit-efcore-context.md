---
step: "001"
title: "Codebase Audit: EF Core DbContext, Factory, and Engine Coupling"
status: "completed"
created_at: "2026-08-24T09:59:50+02:00"
---

# Codebase Audit: EF Core DbContext, Factory, and Engine Coupling

## Purpose

Audit all references to `AppSettingsDbContext`, `AppSettingsDbContextFactory`, `EfCoreStorageEngine`, and related DI/configuration components across `PH.DbAppSettings`, tests, and sample projects.
Document explicit facts and derive architectural deductions to inform the abstract DbContext design.

## Discovered Facts

### 1. Concrete Context Definition

- File: `src/PH.DbAppSettings/Data/AppSettingsDbContext.cs`
- The class is declared as `public class AppSettingsDbContext(DbContextOptions<AppSettingsDbContext> options) : DbContext(options)`.
- It directly exposes `public DbSet<AppSettingEntry> AppSettings => Set<AppSettingEntry>();`.
- In `OnModelCreating`, it applies `AppSettingEntryConfiguration`.

### 2. Design-Time Factory Hardcoding

- File: `src/PH.DbAppSettings/Data/AppSettingsDbContextFactory.cs`
- Implements `IDesignTimeDbContextFactory<AppSettingsDbContext>`.
- Hardcodes `.UseSqlite("Data Source=designtime.db")` to create a concrete `AppSettingsDbContext`.

### 3. Engine and Configuration Hardcoding

- File: `src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs`:
  - Holds `Func<AppSettingsDbContext> _contextFactory`.
  - Accepts `AppSettingsDbContext` or `Func<AppSettingsDbContext>`.
- File: `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`:
  - `CreateDefaultEfEngine()` constructs `DbContextOptionsBuilder<AppSettingsDbContext>().UseSqlite(_options.ConnectionString)`.
- File: `src/PH.DbAppSettings/DbAppSettingsMutableOptions.cs`:
  - `UseEntityFramework` binds to `Action<DbContextOptionsBuilder<AppSettingsDbContext>>`.
  - `UseEntityFrameworkSqlite` hardcodes SQLite.
- File: `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`:
  - `AddDbAppSettingsServices` registers `services.AddDbContext<AppSettingsDbContext>` with SQLite and instantiates `EfCoreStorageEngine` via SQLite factory.
- File: `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`:
  - Fallback constructor calls `new DbContextOptionsBuilder<AppSettingsDbContext>().UseSqlite(options.ConnectionString)`.
- File: `src/PH.DbAppSettings/Services/SeedService.cs`:
  - Constructor overload accepts concrete `AppSettingsDbContext dbContext`.

### 4. Test Harness and Examples Coupling

- File: `tests/PH.DbAppSettings.Tests/EfCoreStorageEngineTests.cs`:
  - Uses `DbContextOptionsBuilder<AppSettingsDbContext>().UseSqlite(_connection)`.
- File: `tests/PH.DbAppSettings.Tests/Helpers/DbContextHelper.cs`:
  - Returns `AppSettingsDbContext` using SQLite in-memory database.
- File: `examples/PH.DbAppSettings.Example.MinimalApi/Program.cs`:
  - Calls `options.UseEntityFrameworkSqlite(connectionString)` creating an isolated SQLite database file `App_Data/appsettings.db`.

## Deductions and Analysis

- **Deduction 1 (Abstract Class Requirement)**: Making `AppSettingsDbContext` public abstract requires changing constructors from `DbContextOptions<AppSettingsDbContext>` to non-generic `DbContextOptions` (or supporting both generic `DbContextOptions<T>` and `DbContextOptions`) so derived classes (e.g. `AppDbContext : AppSettingsDbContext`) can pass their own `DbContextOptions<AppDbContext>`.
- **Deduction 2 (Factory Removal or Generalization)**: An abstract class cannot be instantiated by `IDesignTimeDbContextFactory<AppSettingsDbContext>`. Design-time migrations for host applications belong in the host project using the host's own `DbContext` and factory. The library should not maintain a hardcoded SQLite factory for an abstract class.
- **Deduction 3 (Polymorphic Storage Engine)**: `EfCoreStorageEngine` can remain typed against `AppSettingsDbContext` because any derived context `TContext : AppSettingsDbContext` is assignable to `AppSettingsDbContext`. However, the factory delegate `Func<AppSettingsDbContext>` must allow instantiation of the derived application context.
- **Deduction 4 (Single Database Architecture)**: By co-locating `AppSettings` in the host application's DbContext, configuration tables share the application connection string, migrations, transaction boundaries, and target database provider (PostgreSQL via Npgsql, SQL Server via Microsoft.EntityFrameworkCore.SqlServer, MySQL via Pomelo, or SQLite).

## Handoff

- Findings: `AppSettingsDbContext` is currently tightly coupled to a concrete class and hardcoded SQLite configurations across engine, provider, DI extensions, reload services, and tests.
- Confidence: high.
- Assumptions: The host application wants `AppSettings` to live in its own existing database schema alongside its business entities.
- Open questions: Should `AppSettingsDbContext` be a non-generic abstract class `public abstract class AppSettingsDbContext : DbContext` or also provide a generic variant `public abstract class AppSettingsDbContext<TContext> : DbContext where TContext : DbContext`?
