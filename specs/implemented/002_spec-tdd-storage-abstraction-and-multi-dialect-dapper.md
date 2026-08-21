---
title: "Specification: Storage Engine Abstraction and Multi-Dialect Dapper Engine"
version: "1.0.0"
date_created: "2026-08-21 10:44:00"
last_updated: "2026-08-21 10:44:00"
tags: [tdd, dotnet, storage, dapper, sql-dialects]
git_commit: ""
git_branch: "main"
status: completed
related_specs: ["specs/001_spec-tdd-key-normalization-and-options-binding.md"]
supersedes: []
source_purpose: "Implement IDbAppSettingsStorageEngine abstraction, AppSettingRecord with UpdatedAt timestamp, ISqlDialect generators for SQL Server, PostgreSQL, SQLite, MySQL, and DapperStorageEngine."
---

# Specification: Storage Engine Abstraction and Multi-Dialect Dapper Engine

## 1. Purpose & Scope

### 1.1 Problem Statement

The current library is tightly coupled to Entity Framework Core and hardcoded to SQLite.
Enterprise users require high-performance, lightweight database queries via **Dapper** micro-ORM across multiple SQL database dialects (SQL Server, PostgreSQL, SQLite, MySQL) without pulling in EF Core runtime dependencies when Dapper is preferred.

### 1.2 In-Scope

- Definition of `IDbAppSettingsStorageEngine` interface.
- Definition of `AppSettingRecord` record with `Key`, `Environment`, `Value`, `IsEncrypted`, `UpdatedAt`.
- Definition of `ISqlDialect` and concrete dialect implementations: `SqlServerDialect`, `PostgreSqlDialect`, `SqliteDialect`, `MySqlDialect`.
- Implementation of `DapperStorageEngine` executing parameterized queries and idempotent schema DDL creation.
- TDD unit and integration tests for all dialects and Dapper storage operations on SQLite.

### 1.3 Out-of-Scope

- EF Core adapter implementation (handled in Spec 003).
- CLI tool implementation (handled in Spec 004).

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `IDbAppSettingsStorageEngine` | Core persistence abstraction for retrieving and writing configuration records. |
| `ISqlDialect` | Strategy interface generating dialect-specific SQL syntax for escaping, types, and upserts. |
| `DapperStorageEngine` | Micro-ORM storage engine executing queries via `DbConnection` factory and Dapper. |
| `Idempotent DDL` | SQL table creation statement that succeeds safely even if the table already exists. |

---

## 3. Requirements & Constraints

- **REQ-001**: `IDbAppSettingsStorageEngine` MUST declare `GetAllAsync`, `GetByKeyAsync`, `UpsertAsync`, `UpsertBatchAsync`, `DeleteAsync`, `IsEmptyAsync`, `EnsureSchemaCreatedAsync`, and `GetLastModifiedTimestampAsync`.
- **REQ-002**: `ISqlDialect` MUST correctly quote identifiers (`[]` for SQL Server, `""` for PostgreSQL/SQLite, ``` `` ``` for MySQL).
- **REQ-003**: `ISqlDialect` MUST generate valid dialect-specific upsert statements (`MERGE` for SQL Server, `ON CONFLICT DO UPDATE` for PostgreSQL/SQLite, `ON DUPLICATE KEY UPDATE` for MySQL).
- **REQ-004**: `DapperStorageEngine` MUST use parameterized queries for all operations to prevent SQL injection.
- **REQ-005**: `DapperStorageEngine.EnsureSchemaCreatedAsync` MUST execute the dialect's idempotent table creation script.
- **PERF-001**: `GetLastModifiedTimestampAsync` MUST execute a single scalar query (`SELECT MAX(UpdatedAt)`) in $O(1)$ time.

---

## 4. Architecture & Interfaces

### 4.1 Storage Engine Contract

```csharp
namespace PH.DbAppSettings.Storage;

public interface IDbAppSettingsStorageEngine
{
    Task<IReadOnlyList<AppSettingRecord>> GetAllAsync(string environment, CancellationToken ct = default);
    Task<AppSettingRecord?> GetByKeyAsync(string key, string environment, CancellationToken ct = default);
    Task UpsertAsync(AppSettingRecord entry, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<AppSettingRecord> entries, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, string environment, CancellationToken ct = default);
    Task<bool> IsEmptyAsync(string environment, CancellationToken ct = default);
    Task EnsureSchemaCreatedAsync(CancellationToken ct = default);
    Task<DateTimeOffset?> GetLastModifiedTimestampAsync(string environment, CancellationToken ct = default);
}
```

### 4.2 SQL Dialect Contract

```csharp
namespace PH.DbAppSettings.Storage;

public interface ISqlDialect
{
    string DialectName { get; }
    string EscapeIdentifier(string identifier);
    string GetCreateTableSql(string schema, string table);
    string GetSelectAllSql(string schema, string table);
    string GetSelectByKeySql(string schema, string table);
    string GetUpsertSql(string schema, string table);
    string GetDeleteSql(string schema, string table);
    string GetMaxUpdatedAtSql(string schema, string table);
    string GetIsEmptySql(string schema, string table);
}
```

---

## 5. Dependencies & Integrations

- `Dapper` (2.x)
- `System.Data.Common`

---

## 6. Acceptance Criteria

- **AC-001**:
  - **Given**: `SqlServerDialect`, `PostgreSqlDialect`, `SqliteDialect`, `MySqlDialect`.
  - **When**: `GetUpsertSql("dbo", "AppSettings")` is requested from each dialect.
  - **Then**: Returns valid, syntax-correct upsert statements matching the target dialect engine.
  - **RED Failure Mode**: Compilation error due to missing dialect classes or invalid SQL generated.

- **AC-002**:
  - **Given**: An in-memory SQLite connection and `DapperStorageEngine` with `SqliteDialect`.
  - **When**: `EnsureSchemaCreatedAsync` is invoked on an empty database.
  - **Then**: `AppSettings` table is created with `Key`, `Environment`, `Value`, `IsEncrypted`, `UpdatedAt` columns and composite primary key.
  - **RED Failure Mode**: Method throws `NotImplementedException` or SQL syntax error.

- **AC-003**:
  - **Given**: An initialized `DapperStorageEngine` instance.
  - **When**: `UpsertAsync`, `GetAllAsync`, `GetLastModifiedTimestampAsync`, and `DeleteAsync` are executed in sequence.
  - **Then**: Records are stored, retrieved, timestamped, and deleted accurately without data corruption.
  - **RED Failure Mode**: Operations fail with query execution or mapping errors.

---

## 7. Test Automation Strategy

### 7.1 Test Execution Commands

- **Dialect SQL Unit Tests (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~SqlDialectTests`
- **Dapper Storage Engine Tests (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~DapperStorageEngineTests`
- **Full Suite Verification (REFACTOR)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`

---

## 8. Examples & Edge Cases

### 8.1 SQLite Dialect Upsert SQL

```sql
INSERT INTO [AppSettings] ([Key], [Environment], [Value], [IsEncrypted], [UpdatedAt])
VALUES (@Key, @Environment, @Value, @IsEncrypted, @UpdatedAt)
ON CONFLICT ([Key], [Environment]) DO UPDATE SET
    [Value] = excluded.[Value],
    [IsEncrypted] = excluded.[IsEncrypted],
    [UpdatedAt] = excluded.[UpdatedAt];
```

---

## 9. Spec Validation & AI-Readiness

- [X] Given/When/Then acceptance criteria with explicit RED failure modes.
- [X] Specific test execution commands.
- [X] Task breakdown structured into explicit TDD Triads.
- [X] Comply with `.agents/rules/markdown-style-ai.md`.

---

## 10. References & Instructions

- `.agents/rules/csharp-coding-standards.md`
- `.agents/skills/test-driven-development/SKILL.md`

---

## 11. Task Breakdown (TDD Triads)

```yaml
tasks:
  - id: TASK-003-RED
    title: "Write Failing Unit Tests for ISqlDialect Implementations"
    type: test
    phase: RED
    priority: critical
    objective: "Author SqlDialectTests verifying generated DDL, upsert, query, and timestamp SQL across SQL Server, PostgreSQL, SQLite, MySQL."
    acceptance_criteria:
      - "AC: Test fails due to missing dialect implementations."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/SqlDialectTests.cs"
        reason: "TDD test fixture for SQL dialects."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~SqlDialectTests"

  - id: TASK-003-GREEN
    title: "Implement ISqlDialect and Concrete Dialects"
    type: code
    phase: GREEN
    priority: critical
    objective: "Implement ISqlDialect, SqlServerDialect, PostgreSqlDialect, SqliteDialect, MySqlDialect."
    acceptance_criteria:
      - "AC: SqlDialectTests passes 100% of test cases."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/ISqlDialect.cs"
        reason: "Dialect contract interface."
      - path: "src/PH.DbAppSettings/Storage/Dialects/SqlServerDialect.cs"
        reason: "SQL Server dialect implementation."
      - path: "src/PH.DbAppSettings/Storage/Dialects/PostgreSqlDialect.cs"
        reason: "PostgreSQL dialect implementation."
      - path: "src/PH.DbAppSettings/Storage/Dialects/SqliteDialect.cs"
        reason: "SQLite dialect implementation."
      - path: "src/PH.DbAppSettings/Storage/Dialects/MySqlDialect.cs"
        reason: "MySQL dialect implementation."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~SqlDialectTests"

  - id: TASK-003-REFACTOR
    title: "Refactor SQL Dialects"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Extract common base SQL builder logic and ensure clean formatting."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Storage/ISqlDialect.cs"
        reason: "Refactor common dialect logic."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"

  - id: TASK-004-RED
    title: "Write Failing Integration Tests for DapperStorageEngine"
    type: test
    phase: RED
    priority: critical
    objective: "Author DapperStorageEngineTests exercising table creation, CRUD, batch upserts, and timestamp checks on SQLite."
    acceptance_criteria:
      - "AC: Test fails due to missing DapperStorageEngine class."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/DapperStorageEngineTests.cs"
        reason: "TDD test fixture for DapperStorageEngine."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~DapperStorageEngineTests"

  - id: TASK-004-GREEN
    title: "Implement DapperStorageEngine"
    type: code
    phase: GREEN
    priority: critical
    objective: "Add Dapper package reference to PH.DbAppSettings.csproj and implement DapperStorageEngine."
    acceptance_criteria:
      - "AC: DapperStorageEngineTests passes all test cases."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/IDbAppSettingsStorageEngine.cs"
        reason: "Storage engine contract."
      - path: "src/PH.DbAppSettings/Storage/AppSettingRecord.cs"
        reason: "Unified record model."
      - path: "src/PH.DbAppSettings/Storage/DapperStorageEngine.cs"
        reason: "Dapper engine implementation."
    files_to_modify:
      - path: "src/PH.DbAppSettings/PH.DbAppSettings.csproj"
        reason: "Add Dapper package reference."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~DapperStorageEngineTests"

  - id: TASK-004-REFACTOR
    title: "Refactor DapperStorageEngine"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Ensure robust connection management and proper async cancellation token forwarding."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Storage/DapperStorageEngine.cs"
        reason: "Async connection lifecycle optimization."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"
```

---

## 12. Conflict Detection

- **Conflict Analysis**: Decouples direct `AppSettingsDbContext` references without breaking existing EF Core capabilities.
- **Resolution**: Both Dapper and EF Core will implement the unified `IDbAppSettingsStorageEngine`.

---

## 13. Files Added to Context

- `src/PH.DbAppSettings/PH.DbAppSettings.csproj`
