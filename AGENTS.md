# AGENTS.md - Repository Instructions for PH.DbAppSettings

## Project Overview

`PH.DbAppSettings` is a high-performance .NET 10 configuration provider library that replaces `appsettings.json` in production by persisting settings in relational databases using either **Entity Framework Core 10** or **Dapper**, featuring a unified embedded In-App CLI engine and MSBuild targets.

## Solution Structure

- [PH.DbAppSettings](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/PH.DbAppSettings.csproj): Core configuration provider library, storage engine abstractions, EF Core and Dapper engines, encryption, services, and unified CLI engine (`PH.DbAppSettings.Cli`).
- [PH.DbAppSettings.Tests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj): Unit and integration tests written in xUnit.
- [PH.DbAppSettings.Example.MinimalApi](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/examples/PH.DbAppSettings.Example.MinimalApi/PH.DbAppSettings.Example.MinimalApi.csproj): Example ASP.NET Core Minimal API with OpenAPI and In-App CLI hook.

## Architectural Principles & Invariants

1. **Target Framework & Language**: .NET 10.0 (`net10.0`), C# 14.
2. **Single Assembly & Package**: All runtime configuration, storage engines, and CLI tools are unified in the single `PH.DbAppSettings` package.
3. **Storage Engine Abstraction**: All database interactions must implement [IDbAppSettingsStorageEngine](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/IDbAppSettingsStorageEngine.cs).
4. **Dual Engine Support**: Both Entity Framework Core ([EfCoreStorageEngine](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs)) and Dapper ([DapperStorageEngine](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/DapperStorageEngine.cs)) are supported in the core library assembly.
5. **Multi-Dialect SQL Generation**: Support SQL Server, PostgreSQL, SQLite, and MySQL via [ISqlDialect](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Storage/ISqlDialect.cs).
6. **Key Normalization & Microsoft Options Binding**: Keys stored in the database can use `:` or `__`. In `IConfigurationProvider.Data`, keys are always normalized to `:` via [KeyNormalizer](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Configuration/KeyNormalizer.cs) to ensure native `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` binding works out-of-the-box.
7. **Change Detection**: Change detection in [ReloadBackgroundService](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Services/ReloadBackgroundService.cs) is $O(1)$ based on querying `MAX(UpdatedAt)` instead of full table diffing.
8. **Security**: Sensitive values are encrypted at rest using AES-GCM 256-bit ([AesGcmValueEncryptor](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings/Encryption/AesGcmValueEncryptor.cs)). Database connection strings must never be stored in the database.

## Essential CLI & Build Commands

```bash
# Build full solution
dotnet build PH.DbAppSettings.slnx

# Run all unit and integration tests
dotnet test PH.DbAppSettings.slnx

# Format code according to .NET conventions
dotnet format PH.DbAppSettings.slnx

# Run In-App CLI commands directly via host application
dotnet run --project examples/PH.DbAppSettings.Example.MinimalApi -- dbappsettings analyze appsettings.json
dotnet run --project examples/PH.DbAppSettings.Example.MinimalApi -- dbappsettings import appsettings.json -e Production
dotnet run --project examples/PH.DbAppSettings.Example.MinimalApi -- dbappsettings export appsettings.exported.json
dotnet run --project examples/PH.DbAppSettings.Example.MinimalApi -- dbappsettings rewrite-json appsettings.rewritten.json
# Pack release packages guarded by git branch check
./publish.sh
```

## Code Style & Development Rules

- Always follow strict **Test-Driven Development (TDD)**: Red $\rightarrow$ Green $\rightarrow$ Refactor.
- Enable nullable reference types (`#nullable enable`) and treat warnings as errors.
- Prefer `sealed` classes and `record` types for models and DTOs.
- Use structured logging via `ILogger<T>`.
- Use primary constructors where clean and readable.
