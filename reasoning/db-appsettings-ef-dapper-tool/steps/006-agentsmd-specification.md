---
title: "Step 006: AGENTS.md Architecture and Specification"
step_number: 6
author: "Orchestrator"
experts_involved: ["architecture-expert"]
reasoning_path: "reasoning/db-appsettings-ef-dapper-tool"
---

# Step 006: AGENTS.md Architecture and Specification

## Purpose

Define the structure and complete content specifications for repository and project-level `AGENTS.md` files to guide AI agents and developers working on `PH.DbAppSettings`.

## Architecture of AGENTS.md Files

- **Root `AGENTS.md`**: Provides repository-wide architecture principles, build/test commands, security guidelines, and coding standards.
- **Library `src/PH.DbAppSettings/AGENTS.md`**: Details component architecture, storage engine abstractions (`IDbAppSettingsStorageEngine`), Dapper/EF Core rules, key normalization rules, and encryption policies.
- **Test Suite `tests/PH.DbAppSettings.Tests/AGENTS.md`**: Outlines testing standards, dialect verification, xUnit best practices, and integration test setup.

## Specification 1: Root `AGENTS.md` Content

```markdown
# AGENTS.md - Repository Instructions for PH.DbAppSettings

## Project Overview

PH.DbAppSettings is a high-performance .NET 10 configuration provider library that replaces `appsettings.json` in production by persisting settings in relational databases using either Entity Framework Core 10 or Dapper.

## Architecture Guidelines

- Target framework: `net10.0`, C# 14.
- Storage engine abstraction: All database operations must flow through `IDbAppSettingsStorageEngine`.
- Dual engine support: Must support both Entity Framework Core and Dapper micro-ORM seamlessly.
- Multi-dialect support: SQL Server, PostgreSQL, SQLite, MySQL.
- Key format: Normalized to `:` in `IConfigurationProvider.Data` for native `IOptions<T>` binding.
- Security: Secrets encrypted at rest using AES-GCM 256-bit; connection strings never stored in DB.

## Build and Test Commands

- Build solution: `dotnet build PH.DbAppSettings.slnx`
- Run test suite: `dotnet test PH.DbAppSettings.slnx`
- Format code: `dotnet format PH.DbAppSettings.slnx`

## Code Standards

- Treat warnings as errors; enable nullable reference types (`#nullable enable`).
- Use sealed classes and records where applicable.
- Use structured logging via `ILogger<T>`.
- Use primary constructors where clean and readable.
```

## Specification 2: Library `src/PH.DbAppSettings/AGENTS.md` Content

```markdown
# AGENTS.md - src/PH.DbAppSettings

## Component Boundaries

- `Configuration/`: Contains `DbAppSettingsProvider` and `DbAppSettingsConfigurationSource`. Must normalize all keys to `:` delimiter.
- `Storage/`: Contains `IDbAppSettingsStorageEngine`, `EfCoreStorageEngine`, `DapperStorageEngine`, `ISqlDialect` implementations.
- `Data/`: Contains EF Core `AppSettingsDbContext`, entity models, and migrations.
- `Encryption/`: Contains `IValueEncryptor` and `AesGcmValueEncryptor`.
- `Services/`: Contains `DbAppSettingsWriter`, `SeedService`, and `ReloadBackgroundService`.

## Invariant Rules

- Never bypass `IDbAppSettingsStorageEngine` from `DbAppSettingsProvider` or `DbAppSettingsWriter`.
- Ensure all queries are parameterized to prevent SQL injection.
- Do not add external database driver packages to `PH.DbAppSettings` core dependencies beyond standard relational and Dapper packages.
```

## Specification 3: Test Suite `tests/PH.DbAppSettings.Tests/AGENTS.md` Content

```markdown
# AGENTS.md - tests/PH.DbAppSettings.Tests

## Testing Guidelines

- Framework: xUnit 2.x / 3.x with FluentAssertions/standard Assert.
- Verify native `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` binding.
- Test all 4 SQL dialects (SQL Server, PostgreSQL, SQLite, MySQL) for query and DDL accuracy.
- Use SQLite in-memory for unit and fast integration tests.
```

## Handoff

- **Findings**: Full specifications for root and subproject `AGENTS.md` documents defined.
- **Confidence**: high.
- **Assumptions**: The orchestrator will list explicit instructions for creating these files in the post-flow changes section of `README.md`.
- **Open questions**: None.
