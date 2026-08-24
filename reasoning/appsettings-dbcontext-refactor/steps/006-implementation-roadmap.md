---
step: "006"
title: "Phased Implementation Roadmap"
status: "completed"
created_at: "2026-08-24T10:13:45+02:00"
---

# Phased Implementation Roadmap

## Purpose

Provide a structured, phased implementation roadmap for executing the refactoring of `AppSettingsDbContext` and `AppSettingsDbContextFactory` following strict Test-Driven Development (TDD).

## Roadmap Phases

### Phase 1: Abstract DbContext and Design-Time Factory Base

- **Task 1.1**: Define `public abstract class AppSettingsDbContext : DbContext` with constructors for `DbContextOptions` and parameterless design-time constructor.
- **Task 1.2**: Define generic `public abstract class AppSettingsDbContext<TContext> : AppSettingsDbContext where TContext : DbContext` with constructors for `DbContextOptions<TContext>` and parameterless.
- **Task 1.3**: Implement `public abstract class AppSettingsDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : AppSettingsDbContext` supporting standard connection string discovery and abstract `ConfigureOptionsBuilder(DbContextOptionsBuilder<TContext>, string connectionString)`.
- **Task 1.4**: Remove obsolete concrete `AppSettingsDbContextFactory.cs` and outdated migration snapshot files in `src/PH.DbAppSettings/Data/Migrations/`.

### Phase 2: Storage Engine and Provider Decoupling

- **Task 2.1**: Update `EfCoreStorageEngine` to support `bool useMigrations = false`. In `EnsureSchemaCreatedAsync`, invoke `context.Database.MigrateAsync(ct)` when `useMigrations` is true, otherwise invoke `context.Database.EnsureCreatedAsync(ct)`.
- **Task 2.2**: Ensure `EfCoreStorageEngine` operates cleanly with `Func<AppSettingsDbContext>` and derived instances.
- **Task 2.3**: Remove `CreateDefaultEfEngine()` from `DbAppSettingsProvider.cs`. Throw a descriptive `InvalidOperationException` if no storage engine is configured.

### Phase 3: Generic Options and DI Configuration Extensions

- **Task 3.1**: Add `UseMigrations` property to `DbAppSettingsOptions` and `DbAppSettingsMutableOptions`.
- **Task 3.2**: Add generic overloads `UseEntityFramework<TContext>(Action<DbContextOptionsBuilder<TContext>>)` and `UseEntityFramework<TContext>(Action<DbContextOptionsBuilder<TContext>, string>)` to `DbAppSettingsMutableOptions`.
- **Task 3.3**: Add generic extension methods `AddDbAppSettings<TContext>` and `AddDbAppSettingsServices<TContext>` to `DbAppSettingsExtensions`.
- **Task 3.4**: Remove SQLite-hardcoded fallback constructors from `ReloadBackgroundService` and clean `SeedService` constructors.

### Phase 4: Test Infrastructure Modernization

- **Task 4.1**: Implement `TestAppSettingsDbContext : AppSettingsDbContext<TestAppSettingsDbContext>` in `tests/PH.DbAppSettings.Tests/Helpers/`.
- **Task 4.2**: Refactor `DbContextHelper`, `EfCoreStorageEngineTests`, `DbAppSettingsProviderTests`, `NativeOptionsBindingTests`, `SeedServiceTests`, and `BootstrapIntegrationTests` to use `TestAppSettingsDbContext`.
- **Task 4.3**: Add unit tests for `AppSettingsDesignTimeDbContextFactory<TestAppSettingsDbContext>`, generic `UseEntityFramework<TContext>`, and `UseMigrations` switching.

### Phase 5: Example Project and Documentation Alignment

- **Task 5.1**: Update `examples/PH.DbAppSettings.Example.MinimalApi` to define `AppDbContext : AppSettingsDbContext<AppDbContext>` and register configuration via `AddDbAppSettings<AppDbContext>`.
- **Task 5.2**: Update root `README.md` and `src/PH.DbAppSettings/AGENTS.md` with multi-provider usage examples (PostgreSQL via Npgsql, SQL Server via Microsoft.EntityFrameworkCore.SqlServer, MySQL via Pomelo, SQLite).

## Handoff

- Findings: 5-phase atomic roadmap defined, ready for TDD specification generation via `/tdd-spec-generator`.
- Confidence: high.
- Assumptions: Each phase will follow RED -> GREEN -> REFACTOR cycles.
- Open questions: None.
