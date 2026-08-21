---
title: "Step 007: Synthesis Preparation and Workspace Modification Directives"
step_number: 7
author: "Orchestrator"
experts_involved: ["architecture-expert", "dotnet-config-expert", "data-access-expert"]
reasoning_path: "reasoning/db-appsettings-ef-dapper-tool"
---

# Step 007: Synthesis Preparation and Workspace Modification Directives

## Purpose

Consolidate the entire reasoning process into actionable architecture specifications, flow diagrams, and explicit file modification directives for the final implementation phase.

## System Architecture Diagram

```mermaid
flowchart TD
    subgraph HostApp["Host Application (.NET 10)"]
        ProgramCs["Program.cs (Bootstrap)"]
        AppConfig["IConfiguration / IConfigurationRoot"]
        OptionsBind["IOptions / IOptionsSnapshot / IOptionsMonitor"]
        AppConfig --> OptionsBind
    end

    subgraph CoreProvider["PH.DbAppSettings (Configuration Layer)"]
        Provider["DbAppSettingsProvider"]
        KeyNorm["KeyNormalizer (maps DB keys to ':')"]
        Writer["IDbAppSettingsWriter"]
        Reloader["ReloadBackgroundService (O(1) Timestamp check)"]
        Provider --> KeyNorm
        KeyNorm --> AppConfig
    end

    subgraph StorageLayer["Storage Abstraction Layer"]
        IEngine["IDbAppSettingsStorageEngine"]
        Provider --> IEngine
        Writer --> IEngine
        Reloader --> IEngine
    end

    subgraph Engines["Pluggable Storage Engines"]
        EFEngine["EfCoreStorageEngine (AppSettingsDbContext)"]
        DapperEngine["DapperStorageEngine (ISqlDialect + DbConnection)"]
        IEngine --> EFEngine
        IEngine --> DapperEngine
    end

    subgraph Databases["Supported Database Servers"]
        SqlServer["SQL Server"]
        Postgres["PostgreSQL"]
        Sqlite["SQLite"]
        MySql["MySQL"]
        EFEngine --> Databases
        DapperEngine --> Databases
    end
```

## Explicit External Workspace Modification Directives

As required by reasoning rules, the following actions detail the exact file changes to be applied to the workspace during project completion:

### 1. New Files to Create

- `src/PH.DbAppSettings/Storage/IDbAppSettingsStorageEngine.cs`: Core storage abstraction interface.
- `src/PH.DbAppSettings/Storage/AppSettingRecord.cs`: Unified configuration entry record with `UpdatedAt`.
- `src/PH.DbAppSettings/Storage/ISqlDialect.cs`: SQL dialect interface.
- `src/PH.DbAppSettings/Storage/Dialects/SqlServerDialect.cs`: SQL Server dialect implementation.
- `src/PH.DbAppSettings/Storage/Dialects/PostgreSqlDialect.cs`: PostgreSQL dialect implementation.
- `src/PH.DbAppSettings/Storage/Dialects/SqliteDialect.cs`: SQLite dialect implementation.
- `src/PH.DbAppSettings/Storage/Dialects/MySqlDialect.cs`: MySQL dialect implementation.
- `src/PH.DbAppSettings/Storage/DapperStorageEngine.cs`: High-performance Dapper storage implementation.
- `src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs`: EF Core storage engine implementation.
- `src/PH.DbAppSettings/Configuration/KeyNormalizer.cs`: Bidirectional key converter between DB and `IConfiguration`.
- `AGENTS.md`: Repository-level agent instruction file.
- `src/PH.DbAppSettings/AGENTS.md`: Library-specific agent instruction file.
- `tests/PH.DbAppSettings.Tests/AGENTS.md`: Test-suite agent instruction file.

### 2. Files to Modify

- `src/PH.DbAppSettings/PH.DbAppSettings.csproj`: Add `Dapper` package reference (version 2.x).
- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`: Inject `IDbAppSettingsStorageEngine` and normalize `Data` dictionary keys with `:` delimiter.
- `src/PH.DbAppSettings/DbAppSettingsOptions.cs` & `DbAppSettingsMutableOptions.cs`: Add engine selection and dialect configuration options.
- `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`: Add fluent extensions for `UseEntityFramework` and `UseDapper`.
- `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`: Delegate storage operations to `IDbAppSettingsStorageEngine` and trigger local provider reloads.
- `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`: Modernize polling logic using `GetLastModifiedTimestampAsync`.
- `src/PH.DbAppSettings/Data/AppSettingEntry.cs` & `AppSettingEntryConfiguration.cs`: Add `UpdatedAt` column definition.
- `tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`: Add dialect testing dependencies.
- `README.md`: Update complete documentation for dual EF Core and Dapper usage.

## Handoff

- **Findings**: Synthesis complete. Architecture diagrams and file modification directives defined.
- **Confidence**: high.
- **Assumptions**: The final `README.md` will synthesize the complete reasoning into a self-contained document.
- **Open questions**: None.
