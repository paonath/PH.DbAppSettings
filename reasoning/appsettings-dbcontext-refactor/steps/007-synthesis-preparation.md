---
step: "007"
title: "Synthesis Preparation and Human Modification Instructions"
status: "completed"
created_at: "2026-08-24T10:13:51+02:00"
---

# Synthesis Preparation and Human Modification Instructions

## Purpose

Consolidate the complete reasoning lifecycle, synthesize technical conclusions, and prepare explicit instructions for human and agent workflows transitioning to `/tdd-spec-generator`.

## Core Conclusions

1. **Abstract Base Architecture**:
   - `AppSettingsDbContext` is redefined as `public abstract class AppSettingsDbContext : DbContext` with a companion generic class `public abstract class AppSettingsDbContext<TContext> : AppSettingsDbContext where TContext : DbContext`.
   - Host applications inherit `AppSettingsDbContext` into their own `AppDbContext`, creating the `AppSettings` table directly in the primary application database.
2. **Provider Agnosticism and Connection String Inheritance**:
   - Hardcoded SQLite references are eliminated from the core library.
   - Host applications configure their preferred provider (PostgreSQL via Npgsql, SQL Server via Microsoft.EntityFrameworkCore.SqlServer, MySQL via Pomelo, or SQLite) via generic builder delegates.
   - Connection strings are inherited from host configuration (`ConnectionStrings:DefaultConnection` or `DbAppSettings:ConnectionString`).
3. **Design-Time Factory and Migration Ownership**:
   - Migration snapshots in the core library are removed.
   - Core provides `public abstract class AppSettingsDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : AppSettingsDbContext` to facilitate design-time migration tooling in host applications.
   - Storage schema initialization supports both `EnsureCreatedAsync` (default/lightweight) and `MigrateAsync` (when `UseMigrations = true`).
4. **Decoupled DI and Background Services**:
   - `AddDbAppSettings<TContext>` and `AddDbAppSettingsServices<TContext>` provide typed dependency injection.
   - `ReloadBackgroundService` and `DbAppSettingsProvider` cleanly depend on `IDbAppSettingsStorageEngine` without silent SQLite fallbacks.

## Explicit Modification Instructions for Implementation

When proceeding to `/tdd-spec-generator` and execution:

1. **Refactor Core Data Classes**:
   - Update `src/PH.DbAppSettings/Data/AppSettingsDbContext.cs` to declare the abstract base classes.
   - Replace `src/PH.DbAppSettings/Data/AppSettingsDbContextFactory.cs` with `AppSettingsDesignTimeDbContextFactory<TContext>`.
   - Delete obsolete migration files in `src/PH.DbAppSettings/Data/Migrations/`.
2. **Update Storage Engine & Options**:
   - Update `src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs` to support `UseMigrations` switching.
   - Update `src/PH.DbAppSettings/DbAppSettingsOptions.cs` and `DbAppSettingsMutableOptions.cs` with `UseMigrations` and generic `UseEntityFramework<TContext>`.
3. **Update Provider & DI Extensions**:
   - Update `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs` to remove SQLite fallback.
   - Update `src/PH.DbAppSettings/DbAppSettingsExtensions.cs` to introduce generic `AddDbAppSettings<TContext>` and `AddDbAppSettingsServices<TContext>`.
   - Update `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs` and `SeedService.cs`.
4. **Update Tests & Samples**:
   - Add `TestAppSettingsDbContext` in `tests/PH.DbAppSettings.Tests/Helpers/TestAppSettingsDbContext.cs`.
   - Update all test files in `tests/PH.DbAppSettings.Tests/` to use `TestAppSettingsDbContext`.
   - Update `examples/PH.DbAppSettings.Example.MinimalApi/` to define `AppDbContext : AppSettingsDbContext<AppDbContext>` and configure `Program.cs`.

## Handoff

- Findings: Synthesis complete; ready to generate final standalone `README.md`.
- Confidence: high.
- Assumptions: Ready for final README generation.
- Open questions: None.
