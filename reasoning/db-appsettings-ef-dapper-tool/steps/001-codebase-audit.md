---
title: "Step 001: Comprehensive Codebase and Architecture Audit"
step_number: 1
author: "Orchestrator"
experts_involved: ["dotnet-config-expert", "data-access-expert", "architecture-expert"]
reasoning_path: "reasoning/db-appsettings-ef-dapper-tool"
---

# Step 001: Comprehensive Codebase and Architecture Audit

## Purpose

Audit all existing source code, configuration classes, data models, services, test suites, and documentation within `src/` and `tests/`.
Establish a baseline of current functionality, design patterns, dependencies, and structural assumptions.

## Discovered Facts

### Project and Solution Structure

- Target framework is `net10.0` with C# 14 across `src/PH.DbAppSettings/PH.DbAppSettings.csproj` and `tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`.
- Solution file is `PH.DbAppSettings.slnx` at the repository root.
- Dependencies in `PH.DbAppSettings.csproj`:
  - `Microsoft.EntityFrameworkCore` (10.*)
  - `Microsoft.EntityFrameworkCore.Design` (10.*)
  - `Microsoft.EntityFrameworkCore.Relational` (10.*)
  - `Microsoft.EntityFrameworkCore.Sqlite` (10.*)
  - `Microsoft.Extensions.Configuration` (10.*)
  - `Microsoft.Extensions.Configuration.Abstractions` (10.*)
  - `Microsoft.Extensions.Configuration.Binder` (10.0.8)
  - `Microsoft.Extensions.DependencyInjection` (10.*)
  - `Microsoft.Extensions.Hosting.Abstractions` (10.*)
  - `Microsoft.Extensions.Logging.Abstractions` (10.*)
- Test dependencies in `PH.DbAppSettings.Tests.csproj`:
  - `xunit` (2.9.3), `xunit.runner.visualstudio` (3.1.4), `Microsoft.NET.Test.Sdk` (17.14.1), `coverlet.collector` (6.0.4)
  - `Microsoft.EntityFrameworkCore.InMemory` (10.*), `Microsoft.EntityFrameworkCore.Sqlite` (10.*)

### Existing Source Components

- `src/PH.DbAppSettings/Data/AppSettingEntry.cs`: Entity class with `Key` (required string), `Environment` (string, default `"Production"`), `Value` (nullable string), `IsEncrypted` (bool).
- `src/PH.DbAppSettings/Data/AppSettingEntryConfiguration.cs`: EF Core entity configuration mapping composite primary key `([Key], Environment)`, max lengths 512 for `Key` and 64 for `Environment`, 4000 for `Value`.
- `src/PH.DbAppSettings/Data/AppSettingsDbContext.cs`: EF Core `DbContext` exposing `DbSet<AppSettingEntry> AppSettings`.
- `src/PH.DbAppSettings/Data/AppSettingsDbContextFactory.cs`: Design-time factory hardcoded to `Data Source=designtime.db` via SQLite.
- `src/PH.DbAppSettings/Configuration/DbAppSettingsConfigurationSource.cs`: Implements `IConfigurationSource`, instantiating `DbAppSettingsProvider`.
- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`: Inherits from `ConfigurationProvider`. Executes `LoadAsync` to initialize DB, run EF migrations or `EnsureCreatedAsync`, execute optional seed via `SeedService`, query all records for the current environment, decrypt values if encrypted, and populate base `Data` dictionary.
- `src/PH.DbAppSettings/DbAppSettingsOptions.cs` and `DbAppSettingsMutableOptions.cs`: Configuration options holding `ConnectionString`, `Environment`, `AutoMigrate`, `SeedOnEmpty`, `ForceReseed`, `ExcludeKeysFromSeed`, `EncryptValues`, `ReloadInterval`, `SchemaName`, `TableName`.
- `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`: Provides `AddDbAppSettings` for `IConfigurationBuilder` and `AddDbAppSettingsServices` for `IServiceCollection`.
- `src/PH.DbAppSettings/Encryption/IValueEncryptor.cs` & `AesGcmValueEncryptor.cs`: AES-GCM 256-bit encryption/decryption with Base64 payload encoding (`nonce + tag + ciphertext`).
- `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`: Implements `IDbAppSettingsWriter` with `SetAsync(key, value)`, `SetAsync<T>(key, value)`, and `DeleteAsync(key)` performing upsert/delete via EF Core.
- `src/PH.DbAppSettings/Services/DbAppSettingsReader.cs`: Implements `IDbAppSettingsReader` with `Get<T>(sectionKey)` and `GetValue<T>(key, defaultValue)`.
- `src/PH.DbAppSettings/Services/SeedService.cs`: Flattens bootstrap `IConfiguration` into database keys replacing `:` with `__`, and inserts entries into `AppSettings`.
- `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`: `BackgroundService` polling database on `ReloadInterval` and calling `provider.LoadAsync()` and `provider.TriggerReload()` when snapshot changes.

### Existing Documentation and Governance

- `README.md`: Describes EF Core library usage, connection string bootstrap, options reference, and SQL schema.
- `spec/README.md`: Defines YAML front matter convention for `spec` and `execution-plan` documents.
- `spec/implementation-plan.md`: 17-task implementation plan originally drafted for EF Core-only implementation.
- `AGENTS.md`: No `AGENTS.md` files exist anywhere in the repository.

## Expert Deductions

### `dotnet-config-expert` Findings

- **Key Delimiter Disconnect**: `SeedService` converts `:` to `__` (e.g. `Logging:LogLevel:Default` becomes `Logging__LogLevel__Default`). When `DbAppSettingsProvider` loads entries into `Data`, it stores them using the exact DB key `Logging__LogLevel__Default`. In Microsoft configuration system (`Microsoft.Extensions.Configuration`), hierarchical navigation via `IConfiguration.GetSection("Logging")` expects child keys separated by `:` (`ConfigurationPath.KeyDelimiter`).
- **Standard Options Pattern Breakdown**: Calling `services.Configure<LoggingOptions>(builder.Configuration.GetSection("Logging"))` or using `IOptions<T>` fails to bind nested sections because the configuration provider does not expose `:` delimiters in `Data`.
- **Custom Reader vs Framework Native**: `DbAppSettingsReader` was created to patch this issue by replacing `__` with `:` on ad-hoc queries, but this circumvents native `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` binding.
- **Provider Mutation Seam**: When `DbAppSettingsWriter.SetAsync` writes to DB, it does not notify `DbAppSettingsProvider` or trigger `OnReload()`.

### `data-access-expert` Findings

- **Hardcoded SQLite**: `DbAppSettingsExtensions.AddDbAppSettingsServices`, `DbAppSettingsProvider.LoadAsync`, and `ReloadBackgroundService.DetectChangesAsync` explicitly call `.UseSqlite(...)`. SQL Server, PostgreSQL, and MySQL cannot be used without altering library internals.
- **Absence of Dapper**: There is no Dapper package reference, no Dapper repository implementation, no ADO.NET connection factory, and no SQL query builder for non-EF workflows.
- **Schema Management for Non-EF**: While EF Core supports `AutoMigrate` via migrations, Dapper requires lightweight schema initialization (DDL script execution or idempotency checks) across supported database engines.

### `architecture-expert` Findings

- **Tight Coupling to EF Core**: Core configuration loading directly references `AppSettingsDbContext`. Decoupling requires a clear abstraction (e.g. `IDbAppSettingsStorageEngine` or `IDbSettingsRepository`) implemented separately for EF Core and Dapper.
- **Missing Governance Files**: Root and project-level `AGENTS.md` files are missing and must be defined to establish domain boundaries and development guidelines.
- **Configuration Bootstrap Flow**: The bootstrap connection string strategy (`bootstrapConfig` reading env vars / bootstrap `appsettings.json`) is viable and adheres to security principles avoiding credentials in source files.

## Handoff

- **Findings**: The current codebase provides a functioning SQLite + EF Core configuration provider, but suffers from key delimiter mismatch (`__` instead of `:` in `Data`), hardcoded SQLite calls, zero Dapper support, and lack of `AGENTS.md` governance files.
- **Confidence**: high.
- **Assumptions**: The library must preserve the ability to read configuration into standard `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` without requiring custom wrapper classes like `DbAppSettingsReader`.
- **Open questions**: Should the solution be partitioned into multiple NuGet packages (e.g. `PH.DbAppSettings.Core`, `PH.DbAppSettings.EntityFrameworkCore`, `PH.DbAppSettings.Dapper`) or a single unified package with flexible engine configuration?
