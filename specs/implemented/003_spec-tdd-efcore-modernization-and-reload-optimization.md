---
title: "Specification: EF Core Modernization, Timestamp-Based Reload, and Service Extensions"
version: "1.0.0"
date_created: "2026-08-21 10:44:00"
last_updated: "2026-08-21 10:44:00"
tags: [tdd, dotnet, efcore, reload-service, di-extensions]
git_commit: ""
git_branch: "main"
status: completed
related_specs: ["specs/001_spec-tdd-key-normalization-and-options-binding.md", "specs/002_spec-tdd-storage-abstraction-and-multi-dialect-dapper.md"]
supersedes: []
source_purpose: "Implement EfCoreStorageEngine adapter, modernize AppSettingEntry with UpdatedAt, refactor DbAppSettingsWriter and SeedService to use storage engine, optimize ReloadBackgroundService with timestamp polling, and provide fluent builder extensions."
---

# Specification: EF Core Modernization, Timestamp-Based Reload, and Service Extensions

## 1. Purpose & Scope

### 1.1 Problem Statement

The existing EF Core implementation is hardcoded to SQLite and lacks the `UpdatedAt` timestamp column.
`ReloadBackgroundService` performs an inefficient full-table download and dictionary diff on every interval, and `DbAppSettingsWriter` does not notify the local configuration provider when values change.

### 1.2 In-Scope

- Implementation of `EfCoreStorageEngine` wrapping `AppSettingsDbContext`.
- Modernization of `AppSettingEntry` and `AppSettingEntryConfiguration` to include `UpdatedAt` (`DateTimeOffset?`).
- Refactoring `DbAppSettingsWriter` and `SeedService` to operate through `IDbAppSettingsStorageEngine`.
- Optimization of `ReloadBackgroundService` using `GetLastModifiedTimestampAsync` for $O(1)$ change detection.
- Fluent builder extension methods for `UseEntityFramework` and `UseDapper` on `DbAppSettingsOptions` and `IServiceCollection`.

### 1.3 Out-of-Scope

- CLI tool implementation (handled in Spec 004).

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `EfCoreStorageEngine` | Adapter implementing `IDbAppSettingsStorageEngine` using EF Core `DbContext`. |
| `ReloadBackgroundService` | Hosted background service polling for database changes to trigger `IOptionsMonitor<T>` updates. |
| `UseEntityFramework` | Fluent configuration extension configuring EF Core storage. |
| `UseDapper` | Fluent configuration extension configuring Dapper storage. |

---

## 3. Requirements & Constraints

- **REQ-001**: `EfCoreStorageEngine` MUST implement `IDbAppSettingsStorageEngine` accurately delegating to `AppSettingsDbContext`.
- **REQ-002**: `AppSettingEntry` MUST include `public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;`.
- **REQ-003**: `ReloadBackgroundService` MUST check `GetLastModifiedTimestampAsync` before invoking `provider.LoadAsync()`.
- **REQ-004**: `DbAppSettingsWriter.SetAsync` and `DeleteAsync` MUST update `UpdatedAt` timestamp and optionally trigger immediate local reload on the provider.
- **REQ-005**: `DbAppSettingsExtensions` MUST provide clean overloads for `UseEntityFramework` and `UseDapper`.

---

## 4. Architecture & Interfaces

### 4.1 Fluent Configuration API

```csharp
// Program.cs
builder.Configuration.AddDbAppSettings(bootstrapConfig, options =>
{
    options.Environment = "Production";
    options.ReloadInterval = TimeSpan.FromMinutes(2);
    options.EncryptValues = true;

    // EF Core:
    options.UseEntityFramework(ef => ef.UseSqlServer(connectionString));

    // Or Dapper:
    // options.UseDapper(dapper => dapper.UsePostgres(connectionString));
});
```

---

## 5. Dependencies & Integrations

- `Microsoft.EntityFrameworkCore` (10.*)
- `Microsoft.EntityFrameworkCore.Relational` (10.*)
- `Microsoft.Extensions.Hosting.Abstractions` (10.*)

---

## 6. Acceptance Criteria

- **AC-001**:
  - **Given**: `EfCoreStorageEngine` configured with an in-memory SQLite database.
  - **When**: `UpsertAsync` and `GetAllAsync` are executed.
  - **Then**: Configuration records are saved with `UpdatedAt` timestamps and retrieved accurately.
  - **RED Failure Mode**: Compilation error due to missing `EfCoreStorageEngine` or missing `UpdatedAt` property.

- **AC-002**:
  - **Given**: `ReloadBackgroundService` configured with 1-second interval.
  - **When**: Database entries remain unchanged.
  - **Then**: `provider.LoadAsync()` is NOT called, only `GetLastModifiedTimestampAsync` is polled.
  - **RED Failure Mode**: Provider reloads on every tick regardless of timestamp.

- **AC-003**:
  - **Given**: A registered `IOptionsMonitor<FeatureFlags>` consumer.
  - **When**: A database entry is updated and background reload executes.
  - **Then**: `IOptionsMonitor<FeatureFlags>.CurrentValue` immediately reflects the updated database value.
  - **RED Failure Mode**: `IOptionsMonitor` retains stale startup values.

---

## 7. Test Automation Strategy

### 7.1 Test Execution Commands

- **EF Core Storage Tests (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~EfCoreStorageEngineTests`
- **Timestamp Reload Tests (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~TimestampReloadTests`
- **Full Suite Verification (REFACTOR)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`

---

## 8. Examples & Edge Cases

### 8.1 Timestamp Comparison Logic

```csharp
var latestTimestamp = await _storageEngine.GetLastModifiedTimestampAsync(_options.Environment, ct);
if (latestTimestamp.HasValue && latestTimestamp.Value > _lastSeenTimestamp)
{
    await _provider.LoadAsync(ct);
    _provider.TriggerReload();
    _lastSeenTimestamp = latestTimestamp.Value;
}
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
  - id: TASK-005-RED
    title: "Write Failing Unit Tests for EfCoreStorageEngine"
    type: test
    phase: RED
    priority: high
    objective: "Author EfCoreStorageEngineTests verifying CRUD, batch upsert, and timestamp operations."
    acceptance_criteria:
      - "AC: Test fails due to missing EfCoreStorageEngine class."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/EfCoreStorageEngineTests.cs"
        reason: "TDD test fixture for EfCoreStorageEngine."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~EfCoreStorageEngineTests"

  - id: TASK-005-GREEN
    title: "Implement EfCoreStorageEngine and Update AppSettingEntry"
    type: code
    phase: GREEN
    priority: high
    objective: "Add UpdatedAt to AppSettingEntry and implement EfCoreStorageEngine."
    acceptance_criteria:
      - "AC: EfCoreStorageEngineTests passes all test cases."
    files_to_create:
      - path: "src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs"
        reason: "EF Core engine adapter."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Data/AppSettingEntry.cs"
        reason: "Add UpdatedAt column."
      - path: "src/PH.DbAppSettings/Data/AppSettingEntryConfiguration.cs"
        reason: "Map UpdatedAt column in EF Core."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~EfCoreStorageEngineTests"

  - id: TASK-005-REFACTOR
    title: "Refactor EfCoreStorageEngine"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Optimize DbContext async queries and AsNoTracking usage."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Storage/EfCoreStorageEngine.cs"
        reason: "Query optimization."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"

  - id: TASK-006-RED
    title: "Write Failing Integration Tests for Timestamp-Based Reload"
    type: test
    phase: RED
    priority: high
    objective: "Author TimestampReloadTests verifying O(1) timestamp change detection and IOptionsMonitor update notifications."
    acceptance_criteria:
      - "AC: Test fails because ReloadBackgroundService does not utilize timestamp checks."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/TimestampReloadTests.cs"
        reason: "TDD test fixture for timestamp reload."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~TimestampReloadTests"

  - id: TASK-006-GREEN
    title: "Refactor ReloadBackgroundService, Writer, and Extensions"
    type: code
    phase: GREEN
    priority: high
    objective: "Update ReloadBackgroundService to query GetLastModifiedTimestampAsync, refactor DbAppSettingsWriter, and add UseEntityFramework/UseDapper extensions."
    acceptance_criteria:
      - "AC: TimestampReloadTests turns green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Services/ReloadBackgroundService.cs"
        reason: "Timestamp change detection."
      - path: "src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs"
        reason: "Delegate to IDbAppSettingsStorageEngine."
      - path: "src/PH.DbAppSettings/Services/SeedService.cs"
        reason: "Delegate to IDbAppSettingsStorageEngine."
      - path: "src/PH.DbAppSettings/DbAppSettingsOptions.cs"
        reason: "Add engine builder options."
      - path: "src/PH.DbAppSettings/DbAppSettingsMutableOptions.cs"
        reason: "Add engine builder options."
      - path: "src/PH.DbAppSettings/DbAppSettingsExtensions.cs"
        reason: "Add fluent UseEntityFramework and UseDapper methods."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~TimestampReloadTests"

  - id: TASK-006-REFACTOR
    title: "Refactor Service Extensions and DI Registration"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Ensure clean DI registration for singletons and scoped writers."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/DbAppSettingsExtensions.cs"
        reason: "Clean DI registration."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"
```

---

## 12. Conflict Detection

- **Conflict Analysis**: Replaces old full-table diff in `ReloadBackgroundService` with `O(1)` timestamp check while maintaining full backward compatibility.
- **Resolution**: Deprecate legacy full-table polling.

---

## 13. Files Added to Context

- `src/PH.DbAppSettings/Services/ReloadBackgroundService.cs`
- `src/PH.DbAppSettings/Services/DbAppSettingsWriter.cs`
- `src/PH.DbAppSettings/Data/AppSettingEntry.cs`
