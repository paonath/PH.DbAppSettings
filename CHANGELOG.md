# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-08-21 UTC
Branch: feature/first_rel | Commit: bfc8aca

### Added
- Dual storage engine architecture supporting both **Entity Framework Core 10** (`EfCoreStorageEngine`) and high-performance **Dapper** (`DapperStorageEngine`) in a single package.
- Multi-dialect SQL query and DDL generation for **SQL Server**, **PostgreSQL**, **SQLite**, and **MySQL / MariaDB** via `ISqlDialect`.
- Standalone CLI tool `PH.DbAppSettings.Cli` (`dbappsettings`) with commands:
  - `analyze`: Flattens and audits `appsettings.json`, detecting sensitive properties (passwords, connection strings, keys, secrets, tokens).
  - `import`: Imports flattened JSON configuration entries directly into database tables with automatic schema creation.
  - `export`: Exports database configuration back into structured, indented JSON files.
- Added `KeyNormalizer` for bidirectional mapping between `:` (Microsoft Options standard) and `__` (database/environment safe delimiter).
- Added `UpdatedAt` (`DateTimeOffset`) timestamp column tracking for $O(1)$ change detection.
- Added fluent engine configuration helpers (`UseDapper`, `UseDapperSqlite`, `UseEntityFramework`, `UseEntityFrameworkSqlite`).
- Comprehensive unit and integration test suite with 84 tests (100% green).
- Repository and project-level governance `AGENTS.md` files for root, core library, CLI, and test projects.

### Changed
- Refactored `ReloadBackgroundService` to query `MAX(UpdatedAt)` instead of performing full table diffs.
- Refactored `DbAppSettingsProvider`, `DbAppSettingsWriter`, and `SeedService` to operate over `IDbAppSettingsStorageEngine`.
- Updated `AppSettingsEntryConfiguration` with ISO-8601 string value conversion for SQLite ordering compatibility.
- Updated solution file `PH.DbAppSettings.slnx` to include `PH.DbAppSettings.Cli`.
- Updated `README.md` with complete documentation for dual engines, CLI tool usage, and Options binding.

### Fixed
- Fixed Microsoft Options binding (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) by normalizing configuration keys to `:` delimiter in memory.
