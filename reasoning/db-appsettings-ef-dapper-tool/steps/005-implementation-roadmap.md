---
title: "Step 005: Detailed Implementation Roadmap"
step_number: 5
author: "Orchestrator"
experts_involved: ["architecture-expert", "dotnet-config-expert", "data-access-expert"]
reasoning_path: "reasoning/db-appsettings-ef-dapper-tool"
---

# Step 005: Detailed Implementation Roadmap

## Purpose

Define an atomic, phased implementation roadmap for developing the unified dual-engine `PH.DbAppSettings` tool in .NET 10.
Ensure systematic execution, test verification at each phase, and full architectural compliance.

## Phased Roadmap

### Phase 1: Storage Abstractions, Key Normalization, and Domain Models

- **TASK 1.1 — Bidirectional Key Normalizer**:
  - Implement `KeyNormalizer` static helper.
  - Provide `ToConfigurationKey(string dbKey)` converting `__` or `/` to `:`.
  - Provide `ToDbKey(string configKey)` converting `:` to `__`.
  - Unit test all edge cases (nested sections, arrays, root keys).
- **TASK 1.2 — Unified Domain Record and Storage Interface**:
  - Create `AppSettingRecord` record with `Key`, `Environment`, `Value`, `IsEncrypted`, `UpdatedAt`.
  - Create `IDbAppSettingsStorageEngine` interface with CRUD, batch upsert, timestamp query, and DDL schema initializer.
- **TASK 1.3 — Options Refactoring**:
  - Update `DbAppSettingsOptions` and `DbAppSettingsMutableOptions` to hold engine strategy (`UseEntityFramework` vs `UseDapper`).

### Phase 2: Dapper Storage Engine and Multi-Dialect SQL Generators

- **TASK 2.1 — SQL Dialect Abstraction**:
  - Implement `ISqlDialect` interface.
  - Implement `SqlServerDialect`, `PostgreSqlDialect`, `SqliteDialect`, `MySqlDialect`.
  - Ensure correct identifier quoting, types, and upsert syntax (`MERGE`, `ON CONFLICT DO UPDATE`, `ON DUPLICATE KEY UPDATE`).
- **TASK 2.2 — Dapper Engine Implementation**:
  - Add `Dapper` (2.x / latest compatible) package reference to `PH.DbAppSettings.csproj`.
  - Implement `DapperStorageEngine : IDbAppSettingsStorageEngine` using parameterized queries and `DbConnection` factory.
  - Implement idempotent DDL creation in `EnsureSchemaCreatedAsync`.
- **TASK 2.3 — Dapper Unit and Dialect Tests**:
  - Test SQL generation for all 4 dialects.
  - Test `DapperStorageEngine` against SQLite in-memory connection.

### Phase 3: Entity Framework Core Storage Engine Modernization

- **TASK 3.1 — EF Core Storage Engine Adapter**:
  - Implement `EfCoreStorageEngine : IDbAppSettingsStorageEngine` wrapping `AppSettingsDbContext`.
- **TASK 3.2 — DbContext and Entity Update**:
  - Update `AppSettingEntry` and `AppSettingEntryConfiguration` to include `UpdatedAt` column.
  - Remove hardcoded SQLite calls from `AppSettingsDbContextFactory` and make provider configurable.
- **TASK 3.3 — EF Core Engine Tests**:
  - Verify CRUD and migrations via `EfCoreStorageEngine` on SQLite.

### Phase 4: Configuration Provider and Options Binding

- **TASK 4.1 — Provider Normalization and Storage Engine Integration**:
  - Refactor `DbAppSettingsProvider` to consume `IDbAppSettingsStorageEngine`.
  - Populate `ConfigurationProvider.Data` with normalized `:` keys.
- **TASK 4.2 — Native Options Binding Verification**:
  - Add unit tests verifying `services.Configure<TOptions>(configuration.GetSection(...))`, `IOptions<TOptions>`, `IOptionsSnapshot<TOptions>`, and `IOptionsMonitor<TOptions>`.
- **TASK 4.3 — Timestamp-Optimized Background Reload Service**:
  - Refactor `ReloadBackgroundService` to query `GetLastModifiedTimestampAsync` first.
  - Trigger `provider.LoadAsync()` and `provider.TriggerReload()` only when `UpdatedAt > lastTimestamp`.
- **TASK 4.4 — Writer and Seed Service Refactoring**:
  - Refactor `DbAppSettingsWriter` to use `IDbAppSettingsStorageEngine` and trigger provider reload callback.
  - Refactor `SeedService` to batch upsert bootstrap configuration into the active storage engine.

### Phase 5: Fluent Builder API and DI Extensions

- **TASK 5.1 — Builder Extensions**:
  - Add `options.UseEntityFramework(...)` and `options.UseDapper(...)` extension overloads.
- **TASK 5.2 — Service Registration Extensions**:
  - Modernize `AddDbAppSettingsServices` to register the active storage engine, encryptor, writer, and reload service.

### Phase 6: End-to-End Testing and Verification

- **TASK 6.1 — Integration Test Suite**:
  - Test full bootstrap flow for EF Core and Dapper.
  - Test dynamic reload with `IOptionsMonitor<T>`.
  - Test encryption at rest with AES-GCM.
- **TASK 6.2 — Code Coverage and Static Analysis**:
  - Execute full test suite via `dotnet test`.

### Phase 7: DDL Scripts, Documentation, and Governance

- **TASK 7.1 — DDL SQL Script Library**:
  - Create exportable SQL files for SQL Server, PostgreSQL, SQLite, MySQL in `scripts/`.
- **TASK 7.2 — Update README.md**:
  - Document dual-engine setup, options, and migration from `appsettings.json`.
- **TASK 7.3 — Create AGENTS.md**:
  - Create repository and project-level `AGENTS.md` files.

## Handoff

- **Findings**: Established a complete, 7-phase, 18-task implementation roadmap.
- **Confidence**: high.
- **Assumptions**: Single unified package structure as approved in `qa-midpoint.md`.
- **Open questions**: None.
