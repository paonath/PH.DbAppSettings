# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-08-21 UTC
Branch: feature/first_rel | Commit: 94810a1

### Added
- Example Minimal API project (`examples/PH.DbAppSettings.Example.MinimalApi`) demonstrating **Entity Framework Core 10**, local SQLite database in `App_Data/`, rich nested configuration dataset, and typed Options records hierarchy.
- Added fluent service registration overload `AddDbAppSettingsServices(Action<DbAppSettingsMutableOptions>)`.
- Dual storage engine architecture supporting both **Entity Framework Core 10** (`EfCoreStorageEngine`) and high-performance **Dapper** (`DapperStorageEngine`) in a single package.
- Multi-dialect SQL query and DDL generation for **SQL Server**, **PostgreSQL**, **SQLite**, and **MySQL / MariaDB** via `ISqlDialect`.
- Standalone CLI tool `PH.DbAppSettings.Cli` (`dbappsettings`) with commands:
  - `analyze`: Flattens and audits `appsettings.json`, detecting sensitive properties (passwords, connection strings, keys, secrets, tokens).
  - `import`: Imports flattened JSON configuration entries directly into database tables with automatic schema creation.
  - `export`: Exports database configuration back into structured, indented JSON files.
- Added `KeyNormalizer` for bidirectional mapping between `:` (Microsoft Options standard) and `__` (database/environment safe delimiter).
- Added `UpdatedAt` (`DateTimeOffset`) timestamp column tracking for $O(1)$ change detection.
- Added fluent engine configuration helpers (`UseDapper`, `UseDapperSqlite`, `UseEntityFramework`, `UseEntityFrameworkSqlite`).
- Comprehensive unit and integration test suite with 89 tests (100% green).
- Repository and project-level governance `AGENTS.md` files for root, core library, CLI, and test projects.

### Changed
- Refactored `SeedService` with provider-aware extraction to seed keys exclusively from `JsonConfigurationProvider` / `FileConfigurationProvider`.
- Refactored `ReloadBackgroundService` to query `MAX(UpdatedAt)` instead of performing full table diffs.
- Refactored `DbAppSettingsProvider`, `DbAppSettingsWriter`, and `SeedService` to operate over `IDbAppSettingsStorageEngine`.
- Updated `AppSettingsEntryConfiguration` with ISO-8601 string value conversion for SQLite ordering compatibility.
- Updated solution file `PH.DbAppSettings.slnx` to include `PH.DbAppSettings.Cli` and `PH.DbAppSettings.Example.MinimalApi`.
- Updated `README.md` with complete documentation for dual engines, CLI tool usage, and Options binding.

### Fixed
- Fixed DI constructor resolution for `ReloadBackgroundService` and `DbAppSettingsWriter` when hosted inside ASP.NET Core applications.
- Fixed Microsoft Options binding (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) by normalizing configuration keys to `:` delimiter in memory.

### Security
- Excluded operating system environment variables, process variables, and secrets from being persisted into the database table during seeding, while preserving them in memory for Microsoft Configuration layering.
