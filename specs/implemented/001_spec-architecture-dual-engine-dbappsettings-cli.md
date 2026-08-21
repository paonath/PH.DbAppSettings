---
title: "Specification: Dual-Engine Database AppSettings Provider & CLI Import Tool"
version: "1.0.0"
date_created: "2026-08-21 10:42:00"
last_updated: "2026-08-21 10:42:00"
tags: [architecture, dotnet, configuration, dapper, efcore, cli, tdd]
git_commit: ""
git_branch: "main"
status: ready
related_specs: ["spec/implementation-plan.md"]
supersedes: ["spec/implementation-plan.md"]
source_purpose: "Create a dual-engine (EF Core & Dapper) database configuration tool for .NET 10 that replaces appsettings.json, binds natively to IOptions<T>, provides a CLI tool to analyze and import appsettings.json, and follows strict TDD development."
---

# Specification: Dual-Engine Database AppSettings Provider & CLI Tool

## 1. Purpose & Scope

### 1.1 Problem Statement

In production .NET deployments, static `appsettings.json` files introduce security risks by storing sensitive values in plain text on filesystem/container images, lack centralized dynamic hot-reloading without container restarts, and couple configuration updates to redeployments.

### 1.2 In-Scope

- **Dual Storage Engines**: Native support for **Dapper** (high performance, lightweight ADO.NET) and **Entity Framework Core 10** (`DbContext`, EF migrations) behind a unified `IDbAppSettingsStorageEngine` abstraction.
- **Multi-Database Dialect Support**: SQL Server / Azure SQL, PostgreSQL, SQLite, MySQL across both engines.
- **Native Microsoft Configuration Integration**: Seamless integration with `IConfiguration`, `IConfigurationSection`, `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` via automatic key normalization (mapping `:` to `__` in DB and `__` to `:` in memory).
- **Dynamic Hot Reloading**: Timestamp-based change detection (`UpdatedAt` column) with `O(1)` query efficiency and notification of `IOptionsMonitor<T>` consumers.
- **Security & Encryption**: Optional authenticated encryption at rest using AES-GCM 256-bit for sensitive values.
- **CLI Tool (`PH.DbAppSettings.Cli`)**: Command-line interface to analyze `appsettings.json` hierarchies, detect sensitive keys, and import/export configuration directly into database tables.
- **Governance**: Creation of repository and project-level `AGENTS.md` files.
- **TDD Workflow**: Test-Driven Development enforcing strict Red -> Green -> Refactor cycles for all code and tests.

### 1.3 Out-of-Scope

- Non-relational / NoSQL configuration stores (e.g. MongoDB, Redis).
- Graphical Web UI dashboard (can be built as a separate downstream project using `IDbAppSettingsWriter`).

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `IConfiguration` | .NET core abstraction representing hierarchical key/value application settings. |
| `IOptions<T>` | Singleton options accessor evaluated once at startup. |
| `IOptionsSnapshot<T>` | Scoped options accessor recomputed per DI scope/request. |
| `IOptionsMonitor<T>` | Singleton options accessor listening for reload notifications via `IChangeToken`. |
| `KeyDelimiter` | The standard `:` string in .NET used to separate hierarchical configuration levels. |
| `Dapper` | High-performance micro-ORM executing direct parameterized SQL queries on `DbConnection`. |
| `EF Core` | Object-relational mapper for .NET supporting change tracking and migrations. |
| `AES-GCM` | Authenticated encryption standard combining Galois Counter Mode with AES 256-bit keys. |
| `TDD` | Test-Driven Development workflow (Red -> Green -> Refactor). |

---

## 3. Requirements & Constraints

### 3.1 Functional Requirements

- **REQ-001**: System MUST provide an `IConfigurationProvider` that loads configuration keys from relational database tables.
- **REQ-002**: System MUST normalize all keys loaded from database into `:` notation in `ConfigurationProvider.Data` so `IConfiguration.GetSection(...)` and `IOptions<T>` bind correctly.
- **REQ-003**: System MUST support two pluggable storage engines: `EfCoreStorageEngine` and `DapperStorageEngine`.
- **REQ-004**: System MUST support database dialects for SQL Server, PostgreSQL, SQLite, and MySQL.
- **REQ-005**: System MUST provide `IDbAppSettingsWriter` supporting async upsert (`SetAsync<T>`) and delete (`DeleteAsync`).
- **REQ-006**: System MUST provide `ReloadBackgroundService` that checks for database changes using `MAX(UpdatedAt)` timestamp queries and triggers `OnReload()` only when changes occur.
- **REQ-007**: System MUST provide an interactive CLI tool (`PH.DbAppSettings.Cli`) supporting `analyze`, `import`, and `export` commands.
- **REQ-008**: CLI `analyze` command MUST inspect an existing `appsettings.json` file, display hierarchical keys, detect array indexes, and flag potentially sensitive keys (e.g. passwords, secrets, connection strings).
- **REQ-009**: CLI `import` command MUST parse `appsettings.json`, create database table idempotently if missing, encrypt sensitive keys if enabled, and upsert records into the target database.

### 3.2 Non-Functional & Performance Requirements

- **PERF-001**: Dapper engine startup configuration load MUST execute with minimal allocation and sub-millisecond query time for < 1000 keys.
- **PERF-002**: Change detection polling MUST NOT download full configuration tables, querying only `MAX(UpdatedAt)`.

### 3.3 Security Requirements

- **SEC-001**: System MUST NOT store connection strings in database configuration tables (bootstrap paradox).
- **SEC-002**: When `EncryptValues = true`, values MUST be encrypted with AES-GCM 256-bit before storage in the database.
- **SEC-003**: All SQL queries MUST use parameterized queries or dialect-safe builders to prevent SQL injection.

---

## 4. Architecture & Interfaces

### 4.1 Solution Architecture

```mermaid
flowchart TD
    subgraph HostApp["Host Application (.NET 10)"]
        ProgramCs["Program.cs (Bootstrap)"]
        AppConfig["IConfiguration / IConfigurationRoot"]
        OptionsBind["IOptions<T> / IOptionsSnapshot<T> / IOptionsMonitor<T>"]
        AppConfig --> OptionsBind
    end

    subgraph CoreLibrary["PH.DbAppSettings (Library)"]
        Provider["DbAppSettingsProvider"]
        KeyNorm["KeyNormalizer"]
        Writer["IDbAppSettingsWriter"]
        Reloader["ReloadBackgroundService"]
        Encryptor["IValueEncryptor (AES-GCM)"]
        Provider --> KeyNorm
        KeyNorm --> AppConfig
    end

    subgraph CLI["PH.DbAppSettings.Cli (CLI Tool)"]
        CliMain["Program.cs (CommandLineParser / System.CommandLine)"]
        AnalyzeCmd["AnalyzeCommand"]
        ImportCmd["ImportCommand"]
        ExportCmd["ExportCommand"]
        CliMain --> AnalyzeCmd
        CliMain --> ImportCmd
        CliMain --> ExportCmd
    end

    subgraph StorageLayer["Storage Abstraction Layer"]
        IEngine["IDbAppSettingsStorageEngine"]
        Provider --> IEngine
        Writer --> IEngine
        Reloader --> IEngine
        ImportCmd --> IEngine
        ExportCmd --> IEngine
    end

    subgraph Engines["Pluggable Storage Engines"]
        EFEngine["EfCoreStorageEngine"]
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

### 4.2 Storage Engine Contract (`IDbAppSettingsStorageEngine`)

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

### 4.3 Unified Entity Record (`AppSettingRecord`)

```csharp
namespace PH.DbAppSettings.Storage;

public sealed record AppSettingRecord
{
    public required string Key { get; init; }
    public string Environment { get; init; } = "Production";
    public string? Value { get; init; }
    public bool IsEncrypted { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### 4.4 SQL Dialect Contract (`ISqlDialect`)

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

### 5.1 Projects in Solution

1. `PH.DbAppSettings`: Main library project containing core configuration provider, storage abstraction, Dapper engine, EF Core engine, and encryption.
2. `PH.DbAppSettings.Cli`: Command-line executable project for analyzing, importing, and exporting configuration.
3. `PH.DbAppSettings.Tests`: Unit and integration test suite.

### 5.2 NuGet Dependencies

- `Microsoft.Extensions.Configuration` (10.*)
- `Microsoft.Extensions.Configuration.Binder` (10.*)
- `Microsoft.Extensions.Options.ConfigurationExtensions` (10.*)
- `Microsoft.Extensions.DependencyInjection` (10.*)
- `Microsoft.Extensions.Hosting.Abstractions` (10.*)
- `Microsoft.Extensions.Logging.Abstractions` (10.*)
- `Microsoft.EntityFrameworkCore` (10.*)
- `Microsoft.EntityFrameworkCore.Relational` (10.*)
- `Dapper` (2.x)
- `System.CommandLine` or `Spectre.Console` (for CLI project)

---

## 6. Acceptance Criteria

- **AC-001**: `IConfigurationBuilder.AddDbAppSettings` successfully populates `IConfiguration` with all database keys normalized to `:`.
- **AC-002**: `builder.Services.Configure<TOptions>(builder.Configuration.GetSection("SectionName"))` binds all nested properties and arrays without custom reader calls.
- **AC-003**: Injected `IOptions<TOptions>`, `IOptionsSnapshot<TOptions>`, and `IOptionsMonitor<TOptions>` resolve populated options instances.
- **AC-004**: Modifying a database value updates `IOptionsMonitor<TOptions>.CurrentValue` on background reload interval.
- **AC-005**: Both `UseEntityFramework` and `UseDapper` pass the same functional test suite.
- **AC-006**: CLI tool `dbappsettings analyze` prints formatted tree analysis of `appsettings.json` and flags secrets.
- **AC-007**: CLI tool `dbappsettings import` successfully creates table and populates SQL database from `appsettings.json`.
- **AC-008**: All test cases are authored and executed following strict TDD (Red -> Green -> Refactor) with 100% green test suite.

---

## 7. Test Automation Strategy (TDD)

### 7.1 TDD Workflow Rules

For every unit and integration feature:
1. **Red**: Author failing test in `tests/PH.DbAppSettings.Tests/`. Run test and verify expected failure.
2. **Green**: Implement minimal code to satisfy test. Run test and verify pass.
3. **Refactor**: Clean up and optimize implementation while keeping full test suite green.

### 7.2 Test Categories

- **Key Normalization Tests**: Bidirectional conversion, array indexes, nested colons, invalid keys.
- **Dialect SQL Tests**: Query generation checks for SQL Server, PostgreSQL, SQLite, MySQL.
- **Dapper Storage Engine Tests**: In-memory SQLite CRUD, batch upsert, timestamp checks.
- **EF Core Storage Engine Tests**: In-memory SQLite CRUD, migrations.
- **Options Binding Integration Tests**: `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>` live reload verification.
- **CLI Tool Tests**: JSON parsing, analyze command output, import command execution.

---

## 8. Examples & Edge Cases

### 8.1 CLI Usage Examples

```bash
# 1. Analyze existing appsettings.json
dotnet dbappsettings analyze ./appsettings.json

# 2. Import into SQLite (Development)
dotnet dbappsettings import ./appsettings.json \
  --connection-string "Data Source=appconfig.db" \
  --engine dapper \
  --dialect sqlite \
  --environment Development

# 3. Import into SQL Server with encryption (Production)
dotnet dbappsettings import ./appsettings.Production.json \
  --connection-string "Server=sql.prod;Database=AppConfig;User Id=app;Password=secret;" \
  --engine dapper \
  --dialect sqlserver \
  --environment Production \
  --encrypt \
  --encryption-secret "env:ENCRYPTION_KEY"
```

### 8.2 Application Setup Example

```csharp
var builder = WebApplication.CreateBuilder(args);

var bootstrapConfig = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.Environment = builder.Environment.EnvironmentName;
    options.ReloadInterval = TimeSpan.FromMinutes(2);
    options.EncryptValues = true;
    options.UseDapper(dapper => dapper.UseSqlServer(connectionString));
});

builder.Services.AddDbAppSettingsServices(options);
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("Mail"));
```

---

## 9. Spec Validation & AI-Readiness

- [X] Clear and unambiguous language.
- [X] All acronyms and terms defined.
- [X] MUST/SHALL/SHOULD keywords used.
- [X] Concrete acceptance criteria specified.
- [X] Self-contained context with architectural diagrams.
- [X] Comply with `.agents/rules/markdown-style-ai.md`.

---

## 10. References & Instructions

- `.agents/rules/csharp-coding-standards.md`
- `.agents/rules/dotnet-cli-usage.md`
- `.agents/rules/markdown-style-ai.md`
- `.agents/skills/test-driven-development/SKILL.md`
- `.agents/skills/mermaid-flow-diagrams/SKILL.md`

---

## 11. Task Breakdown (Atomic Implementation Tasks)

```yaml
tasks:
  - id: TASK-001
    title: "Implement KeyNormalizer with Unit Tests (TDD)"
    type: code
    priority: critical
    objective: "Implement bidirectional normalization between ':' and '__' to enable standard IConfiguration section binding."
    preconditions: []
    acceptance_criteria:
      - "AC: Converts 'A:B:C' to 'A__B__C' and 'A__B__C' to 'A:B:C'."
    files_to_create:
      - path: "src/PH.DbAppSettings/Configuration/KeyNormalizer.cs"
        reason: "Key normalization logic."
      - path: "tests/PH.DbAppSettings.Tests/KeyNormalizerTests.cs"
        reason: "TDD unit tests."

  - id: TASK-002
    title: "Implement Storage Engine Abstraction and Record Model"
    type: code
    priority: critical
    objective: "Define IDbAppSettingsStorageEngine interface and AppSettingRecord with UpdatedAt."
    preconditions: ["TASK-001"]
    acceptance_criteria:
      - "AC: Storage interface compiles and exposes complete CRUD, batch, and timestamp operations."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/IDbAppSettingsStorageEngine.cs"
        reason: "Storage engine interface."
      - path: "src/PH.DbAppSettings/Storage/AppSettingRecord.cs"
        reason: "Common record model."

  - id: TASK-003
    title: "Implement Multi-Dialect SQL Generators (TDD)"
    type: code
    priority: high
    objective: "Implement ISqlDialect for SQL Server, PostgreSQL, SQLite, MySQL."
    preconditions: ["TASK-002"]
    acceptance_criteria:
      - "AC: Dialect generators produce valid SQL matching dialect escaping and upsert syntax."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/ISqlDialect.cs"
        reason: "Dialect interface."
      - path: "src/PH.DbAppSettings/Storage/Dialects/SqlServerDialect.cs"
        reason: "SQL Server dialect."
      - path: "src/PH.DbAppSettings/Storage/Dialects/PostgreSqlDialect.cs"
        reason: "PostgreSQL dialect."
      - path: "src/PH.DbAppSettings/Storage/Dialects/SqliteDialect.cs"
        reason: "SQLite dialect."
      - path: "src/PH.DbAppSettings/Storage/Dialects/MySqlDialect.cs"
        reason: "MySQL dialect."
      - path: "tests/PH.DbAppSettings.Tests/SqlDialectTests.cs"
        reason: "TDD dialect tests."

  - id: TASK-004
    title: "Implement DapperStorageEngine (TDD)"
    type: code
    priority: critical
    objective: "Implement high-performance Dapper engine using IDbConnectionFactory and ISqlDialect."
    preconditions: ["TASK-003"]
    acceptance_criteria:
      - "AC: DapperStorageEngine passes CRUD, batch upsert, and schema creation on SQLite."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/DapperStorageEngine.cs"
        reason: "Dapper engine implementation."
      - path: "tests/PH.DbAppSettings.Tests/DapperStorageEngineTests.cs"
        reason: "TDD Dapper storage tests."

  - id: TASK-005
    title: "Implement EfCoreStorageEngine (TDD)"
    type: code
    priority: high
    objective: "Wrap AppSettingsDbContext with IDbAppSettingsStorageEngine."
    preconditions: ["TASK-002"]
    acceptance_criteria:
      - "AC: EfCoreStorageEngine passes CRUD and migration tests."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs"
        reason: "EF Core engine implementation."
      - path: "tests/PH.DbAppSettings.Tests/EfCoreStorageEngineTests.cs"
        reason: "TDD EF Core storage tests."

  - id: TASK-006
    title: "Refactor DbAppSettingsProvider & Options Binding (TDD)"
    type: code
    priority: critical
    objective: "Refactor provider to consume IDbAppSettingsStorageEngine and populate Data with ':' keys."
    preconditions: ["TASK-004", "TASK-005"]
    acceptance_criteria:
      - "AC: IOptions<T>, IOptionsSnapshot<T>, and IOptionsMonitor<T> bind natively."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/NativeOptionsBindingTests.cs"
        reason: "TDD options binding tests."

  - id: TASK-007
    title: "Refactor Writer, Seed, and Timestamp Reload Service (TDD)"
    type: code
    priority: high
    objective: "Refactor DbAppSettingsWriter, SeedService, and ReloadBackgroundService to use storage engine."
    preconditions: ["TASK-006"]
    acceptance_criteria:
      - "AC: ReloadBackgroundService detects updates via MAX(UpdatedAt) without downloading full table."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/TimestampReloadTests.cs"
        reason: "TDD reload tests."

  - id: TASK-008
    title: "Implement CLI Tool (PH.DbAppSettings.Cli) (TDD)"
    type: code
    priority: high
    objective: "Build CLI project supporting analyze, import, export commands."
    preconditions: ["TASK-006", "TASK-007"]
    acceptance_criteria:
      - "AC: CLI tool analyzes appsettings.json and imports keys into SQL database."
    files_to_create:
      - path: "src/PH.DbAppSettings.Cli/PH.DbAppSettings.Cli.csproj"
        reason: "CLI project file."
      - path: "src/PH.DbAppSettings.Cli/Program.cs"
        reason: "CLI entry point."
      - path: "src/PH.DbAppSettings.Cli/Commands/AnalyzeCommand.cs"
        reason: "Analyze command."
      - path: "src/PH.DbAppSettings.Cli/Commands/ImportCommand.cs"
        reason: "Import command."
      - path: "src/PH.DbAppSettings.Cli/Commands/ExportCommand.cs"
        reason: "Export command."
      - path: "tests/PH.DbAppSettings.Tests/CliCommandTests.cs"
        reason: "TDD CLI tests."

  - id: TASK-009
    title: "Create AGENTS.md Governance Files"
    type: documentation
    priority: medium
    objective: "Create repository root, library, CLI, and test AGENTS.md files."
    preconditions: []
    acceptance_criteria:
      - "AC: AGENTS.md files exist and define domain boundaries and rules."
    files_to_create:
      - path: "AGENTS.md"
        reason: "Repository root instructions."
      - path: "src/PH.DbAppSettings/AGENTS.md"
        reason: "Library instructions."
      - path: "src/PH.DbAppSettings.Cli/AGENTS.md"
        reason: "CLI tool instructions."
      - path: "tests/PH.DbAppSettings.Tests/AGENTS.md"
        reason: "Test suite instructions."
```

---

## 12. Conflict Detection

- **Conflict Analysis**: The original `spec/implementation-plan.md` assumed a single SQLite + EF Core implementation and did not account for Dapper or CLI import commands.
- **Resolution**: This specification (`specs/001_spec-architecture-dual-engine-dbappsettings-cli.md`) supersedes `spec/implementation-plan.md`, establishing the dual-engine architecture and CLI tooling.

---

## 13. Files Added to Context

- `README.md`
- `spec/README.md`
- `spec/implementation-plan.md`
- `src/PH.DbAppSettings/PH.DbAppSettings.csproj`
- `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`
- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`
- `src/PH.DbAppSettings/Data/AppSettingsDbContext.cs`
- `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`
- `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`
- `tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`
