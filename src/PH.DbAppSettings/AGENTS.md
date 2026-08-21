# AGENTS.md - src/PH.DbAppSettings

## Project Scope

Core library containing the Microsoft Configuration Provider implementation, storage engine adapters, encryption mechanisms, and background reload services.

## Component Layout & Responsibilities

- **`Configuration/`**:
  - [DbAppSettingsProvider](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs): Custom `ConfigurationProvider` implementing asynchronous configuration loading and reload notification tokens.
  - [DbAppSettingsConfigurationSource](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Configuration/DbAppSettingsConfigurationSource.cs): Implements `IConfigurationSource`.
  - [KeyNormalizer](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Configuration/KeyNormalizer.cs): Normalizes hierarchical keys between `:` (Microsoft Options standard) and `__` (safe database/environment delimiter).
- **`Storage/`**:
  - [IDbAppSettingsStorageEngine](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/IDbAppSettingsStorageEngine.cs): Unified storage interface for CRUD, schema creation, batch upsert, and timestamp queries.
  - [AppSettingRecord](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/AppSettingRecord.cs): Immutable record representing a configuration row with `UpdatedAt` timestamp.
  - [ISqlDialect](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/ISqlDialect.cs): Contract for SQL query generation across SQL Server, PostgreSQL, SQLite, and MySQL.
  - [EfCoreStorageEngine](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs): EF Core adapter implementation.
  - [DapperStorageEngine](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/DapperStorageEngine.cs): High-performance micro-ORM adapter using raw `DbConnection`.
- **`Data/`**:
  - [AppSettingsDbContext](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Data/AppSettingsDbContext.cs): EF Core DbContext.
  - [AppSettingEntry](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Data/AppSettingEntry.cs): Entity model.
  - [AppSettingEntryConfiguration](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Data/AppSettingEntryConfiguration.cs): Fluent mapping configuration with ISO-8601 string value conversion for `UpdatedAt`.
- **`Encryption/`**:
  - [IValueEncryptor](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Encryption/IValueEncryptor.cs): Encryption abstraction.
  - [AesGcmValueEncryptor](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Encryption/AesGcmValueEncryptor.cs): Authenticated AES-GCM 256-bit encryption implementation.
- **`Services/`**:
  - [DbAppSettingsWriter](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs): Writer service for inserting and updating configuration keys with automatic timestamps.
  - [SeedService](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Services/SeedService.cs): Imports initial configuration into an empty database.
  - [ReloadBackgroundService](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Services/ReloadBackgroundService.cs): Background service that polls `MAX(UpdatedAt)` to detect remote configuration changes.

## Development Invariants

- Always keep `DbAppSettingsProvider.Data` keys normalized to `:` delimiter.
- Storage engine calls must always pass parameterized values to prevent SQL injection.
- Do not add driver-specific database packages directly to `PH.DbAppSettings.csproj` (keep it agnostic with relational EF Core and Dapper).
