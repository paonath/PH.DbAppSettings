# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-08-24 UTC
Branch: feature/first_rel

### Added
- Abstract base class `AppSettingsDbContext` and generic base class `AppSettingsDbContext<TContext>` allowing consumer applications to co-locate `AppSettings` configuration tables directly in their primary application database.
- Abstract base design-time factory `AppSettingsDesignTimeDbContextFactory<TContext>` supporting EF Core migrations (`dotnet ef migrations add`) in host applications across multiple relational database providers.
- Generic configuration methods `UseEntityFramework<TContext>` in `DbAppSettingsMutableOptions` supporting provider configuration delegates (PostgreSQL/Npgsql, SQL Server, Pomelo MySQL, SQLite) and connection string resolution.
- Generic extension methods `AddDbAppSettings<TContext>` and `AddDbAppSettingsServices<TContext>` in `DbAppSettingsExtensions` for type-safe DI registration.
- Added `UseMigrations` option to `DbAppSettingsOptions` and `DbAppSettingsMutableOptions` enabling schema initialization via `Database.MigrateAsync()` in production environments.
- Added `TestAppSettingsDbContext` test fixture and unit tests (`AbstractDbContextTests`, `DesignTimeFactoryTests`, `GenericExtensionsTests`) bringing total suite to 104 tests (100% green).
- Example Minimal API project (`examples/PH.DbAppSettings.Example.MinimalApi`) demonstrating **Entity Framework Core 10**, local SQLite database in `App_Data/`, rich nested configuration dataset, and typed Options records hierarchy.
- Added fluent service registration overload `AddDbAppSettingsServices(Action<DbAppSettingsMutableOptions>)`.
- Dual storage engine architecture supporting both **Entity Framework Core 10** (`EfCoreStorageEngine`) and high-performance **Dapper** (`DapperStorageEngine`) in a single package.
- Multi-dialect SQL query and DDL generation for **SQL Server**, **PostgreSQL**, **SQLite**, and **MySQL / MariaDB** via `ISqlDialect`.
- Standalone CLI tool `PH.DbAppSettings.Cli` (`dbappsettings`) with commands:
  - `analyze`: Flattens and audits `appsettings.json`, detecting sensitive properties (passwords, connection strings, keys, secrets, tokens).
  - `import`: Imports flattened JSON configuration entries directly into database tables with automatic schema creation.
  - `ingest`: Imports configuration entries into the database table and safely deletes the source `appsettings.json` file upon confirmation.
  - `export`: Exports database configuration back into structured, indented JSON files.
  - `rewrite-json`: Reconstructs a fully typed and array-structured `appsettings.json` configuration file from the database.
- Added `JsonTreeReconstructor` for reconstructing typed JSON trees (booleans, numbers, arrays) from flattened database records.
- Added standalone application bootstrapping support allowing applications to run purely from database tables without local JSON files.
- Added `KeyNormalizer` for bidirectional mapping between `:` (Microsoft Options standard) and `__` (database/environment safe delimiter).
- Added `UpdatedAt` (`DateTimeOffset`) timestamp column tracking for $O(1)$ change detection.
- Added fluent engine configuration helpers (`UseDapper`, `UseDapperSqlite`, `UseEntityFramework`, `UseEntityFrameworkSqlite`).
- Repository and project-level governance `AGENTS.md` files for root, core library, CLI, and test projects.

### Changed
- Refactored `AppSettingsDbContext` from a concrete SQLite-coupled class to an abstract base class.
- Refactored `EfCoreStorageEngine` to operate polymorphically over `AppSettingsDbContext` and support `UseMigrations` switching.
- Updated `examples/PH.DbAppSettings.Example.MinimalApi` to define `AppDbContext : AppSettingsDbContext<AppDbContext>` and register configuration via `AddDbAppSettings<AppDbContext>`.
- Refactored `SeedService` with provider-aware extraction to seed keys exclusively from `JsonConfigurationProvider` / `FileConfigurationProvider`.
- Refactored `ReloadBackgroundService` to query `MAX(UpdatedAt)` instead of performing full table diffs.
- Refactored `DbAppSettingsProvider`, `DbAppSettingsWriter`, and `SeedService` to operate over `IDbAppSettingsStorageEngine`.
- Updated `AppSettingsEntryConfiguration` with ISO-8601 string value conversion for SQLite ordering compatibility.
- Updated solution file `PH.DbAppSettings.slnx` to include `PH.DbAppSettings.Cli` and `PH.DbAppSettings.Example.MinimalApi`.
- Updated `README.md` with complete documentation for abstract `AppSettingsDbContext`, multi-provider EF Core setup, dual engines, CLI tool usage, and Options binding.

### Removed
- Removed obsolete concrete `AppSettingsDbContextFactory` with hardcoded SQLite and internal migration snapshot files from core library.
- Removed silent SQLite fallback constructors and methods from `DbAppSettingsProvider` and `ReloadBackgroundService`.

### Fixed
- Fixed DI constructor resolution for `ReloadBackgroundService` and `DbAppSettingsWriter` when hosted inside ASP.NET Core applications.
- Fixed Microsoft Options binding (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) by normalizing configuration keys to `:` delimiter in memory.

### Security
- Excluded operating system environment variables, process variables, and secrets from being persisted into the database table during seeding, while preserving them in memory for Microsoft Configuration layering.
