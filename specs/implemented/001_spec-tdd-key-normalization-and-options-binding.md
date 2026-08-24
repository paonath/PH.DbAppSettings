---
title: "Specification: Key Normalization and Native Microsoft Options Binding"
version: "1.0.0"
date_created: "2026-08-21 10:44:00"
last_updated: "2026-08-21 10:44:00"
tags: [tdd, dotnet, configuration, options, key-normalizer]
git_commit: ""
git_branch: "main"
status: completed
related_specs: []
supersedes: []
source_purpose: "Fix the key delimiter mismatch between database storage and Microsoft Configuration, implement KeyNormalizer, and ensure native IOptions<T>, IOptionsSnapshot<T>, and IOptionsMonitor<T> binding works out of the box."
---

# Specification: Key Normalization and Native Microsoft Options Binding

## 1. Purpose & Scope

### 1.1 Problem Statement

`DbAppSettingsProvider` currently loads configuration keys from the database (e.g. `Logging__LogLevel__Default`) into its internal `Data` dictionary without replacing double underscores (`__`) with colons (`:`).
Because `IConfiguration.GetSection("Logging")` filters for child keys prefixed with `Logging:`, child configuration values are not found, breaking native `IOptions<T>`, `IOptionsSnapshot<T>`, and `IOptionsMonitor<T>` binding.

### 1.2 In-Scope

- Implementation of `KeyNormalizer` for bidirectional conversion between database key formats (`__` or `:`) and configuration keys (`:`).
- Refactoring `DbAppSettingsProvider` to normalize all loaded keys to `:` in memory.
- Ensuring `builder.Services.Configure<TOptions>(configuration.GetSection(...))` binds nested POCO classes and positional records.
- TDD test coverage for key normalization and native options binding without custom readers.

### 1.3 Out-of-Scope

- Dapper and EF Core storage engine abstraction (handled in subsequent specs).
- CLI tool implementation (handled in subsequent specs).

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `ConfigurationPath.KeyDelimiter` | The standard `:` delimiter used internally by `Microsoft.Extensions.Configuration`. |
| `DbKey` | Key stored in the database, typically using `__` (e.g. `App__Smtp__Port`). |
| `ConfigKey` | Key used in `IConfiguration`, strictly using `:` (e.g. `App:Smtp:Port`). |
| `IOptions<T>` | Microsoft singleton options pattern binding interface. |

---

## 3. Requirements & Constraints

- **REQ-001**: `KeyNormalizer.ToConfigurationKey` MUST convert any `__` or `/` separators in a key to `:`.
- **REQ-002**: `KeyNormalizer.ToDbKey` MUST convert `:` separators in a key to `__`.
- **REQ-003**: `DbAppSettingsProvider.LoadAsync` MUST normalize all database keys to `:` delimiter before inserting into `ConfigurationProvider.Data`.
- **REQ-004**: System MUST allow `IConfiguration.GetSection(...)` and `services.Configure<T>` to bind all nested properties and collections natively.
- **SEC-001**: Key normalization MUST NOT expose or log secret values in plain text.

---

## 4. Architecture & Interfaces

### 4.1 KeyNormalizer Interface

```csharp
namespace PH.DbAppSettings.Configuration;

public static class KeyNormalizer
{
    public static string ToConfigurationKey(string dbKey);
    public static string ToDbKey(string configKey);
}
```

---

## 5. Dependencies & Integrations

- `Microsoft.Extensions.Configuration` (10.*)
- `Microsoft.Extensions.Configuration.Binder` (10.*)
- `Microsoft.Extensions.Options.ConfigurationExtensions` (10.*)
- `Microsoft.Extensions.DependencyInjection` (10.*)

---

## 6. Acceptance Criteria

- **AC-001**:
  - **Given**: A raw database key with double underscores `Logging__LogLevel__Default`.
  - **When**: `KeyNormalizer.ToConfigurationKey` is invoked.
  - **Then**: Returns `Logging:LogLevel:Default`.
  - **RED Failure Mode**: Compilation failure or returns unnormalized string.

- **AC-002**:
  - **Given**: An `IConfiguration` path `ConnectionStrings:Default`.
  - **When**: `KeyNormalizer.ToDbKey` is invoked.
  - **Then**: Returns `ConnectionStrings__Default`.
  - **RED Failure Mode**: Compilation failure or returns unchanged string.

- **AC-003**:
  - **Given**: Database entries stored as `Smtp__Host` = `"smtp.mail.com"` and `Smtp__Port` = `"587"`.
  - **When**: `DbAppSettingsProvider.Load()` is executed and `services.Configure<SmtpOptions>(config.GetSection("Smtp"))` is called.
  - **Then**: Resolved `IOptions<SmtpOptions>.Value` has `Host == "smtp.mail.com"` and `Port == 587`.
  - **RED Failure Mode**: `IOptions<SmtpOptions>.Value` contains default/empty values (`Host == ""` and `Port == 0`).

---

## 7. Test Automation Strategy

### 7.1 Test Execution Commands

- **Unit Test Execution (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~KeyNormalizerTests`
- **Options Binding Integration (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~NativeOptionsBindingTests`
- **Full Suite Verification (REFACTOR)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`

### 7.2 Testing Guidelines

- Structure all tests strictly following Arrange-Act-Assert (AAA).
- Use in-memory collections and real `ConfigurationBuilder` instances instead of mocking `IConfiguration`.

---

## 8. Examples & Edge Cases

### 8.1 Array and Complex Key Mapping

- `AllowedHosts:0` -> `AllowedHosts__0` -> `AllowedHosts:0`
- `Features:Flags:Beta` -> `Features__Flags__Beta` -> `Features:Flags:Beta`
- `RootKey` -> `RootKey` -> `RootKey`

---

## 9. Spec Validation & AI-Readiness

- [X] All acceptance criteria formatted as Given/When/Then with explicit RED failure modes.
- [X] Test commands explicitly specified for RED, GREEN, and REFACTOR phases.
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
  - id: TASK-001-RED
    title: "Write Failing Unit Tests for KeyNormalizer"
    type: test
    phase: RED
    priority: critical
    objective: "Author KeyNormalizerTests covering bidirectional key conversion, array indexes, and nested sections."
    acceptance_criteria:
      - "AC: Test suite fails with compilation error due to missing KeyNormalizer class."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/KeyNormalizerTests.cs"
        reason: "TDD test fixture for KeyNormalizer."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~KeyNormalizerTests"

  - id: TASK-001-GREEN
    title: "Implement KeyNormalizer"
    type: code
    phase: GREEN
    priority: critical
    objective: "Implement KeyNormalizer.ToConfigurationKey and KeyNormalizer.ToDbKey."
    acceptance_criteria:
      - "AC: KeyNormalizerTests passes all test cases."
    files_to_create:
      - path: "src/PH.DbAppSettings/Configuration/KeyNormalizer.cs"
        reason: "KeyNormalizer static helper implementation."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~KeyNormalizerTests"

  - id: TASK-001-REFACTOR
    title: "Refactor KeyNormalizer"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Optimize string allocation and add argument validation."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Configuration/KeyNormalizer.cs"
        reason: "Allocation optimization."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"

  - id: TASK-002-RED
    title: "Write Failing Integration Test for Native IOptions<T> Binding"
    type: test
    phase: RED
    priority: critical
    objective: "Author NativeOptionsBindingTests verifying IOptions<T>, IOptionsSnapshot<T>, and IOptionsMonitor<T> with DbAppSettingsProvider."
    acceptance_criteria:
      - "AC: Test fails because DbAppSettingsProvider does not normalize '__' to ':' in Data dictionary."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/NativeOptionsBindingTests.cs"
        reason: "TDD test fixture for native options binding."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~NativeOptionsBindingTests"

  - id: TASK-002-GREEN
    title: "Normalize Provider Data Keys in DbAppSettingsProvider"
    type: code
    phase: GREEN
    priority: critical
    objective: "Update DbAppSettingsProvider.LoadAsync to normalize all keys to ':' using KeyNormalizer."
    acceptance_criteria:
      - "AC: NativeOptionsBindingTests turns green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs"
        reason: "Normalize keys before adding to Data dictionary."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~NativeOptionsBindingTests"

  - id: TASK-002-REFACTOR
    title: "Refactor DbAppSettingsProvider"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Ensure clean error logging and remove obsolete manual reader dependencies."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs"
        reason: "Clean up provider methods."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"
```

---

## 12. Conflict Detection

- **Conflict Analysis**: No conflicts with existing codebase; replaces ad-hoc `DbAppSettingsReader` requirement with native .NET Options integration.
- **Resolution**: Deprecate reliance on `DbAppSettingsReader` in favor of standard `IOptions<T>`.

---

## 13. Files Added to Context

- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`
- `src/PH.DbAppSettings/Services/DbAppSettingsReader.cs`
- `tests/PH.DbAppSettings.Tests/TypedReadingTests.cs`
