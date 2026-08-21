---
title: "Specification: Environment Variable & Secret Exclusion from Database Seeding"
version: "1.0.0"
date_created: "2026-08-21 12:33:00"
last_updated: "2026-08-21 12:33:00"
tags: [tdd, dotnet, seeding, security, environment-variables, json-only]
git_commit: "1f193c8"
git_branch: "feature/first_rel"
status: completed
related_specs: [
  "specs/implemented/001_spec-tdd-key-normalization-and-options-binding.md",
  "specs/implemented/002_spec-tdd-storage-abstraction-and-multi-dialect-dapper.md",
  "specs/implemented/003_spec-tdd-efcore-modernization-and-reload-optimization.md",
  "specs/implemented/004_spec-tdd-cli-appsettings-tool.md",
  "specs/implemented/005_spec-governance-agentsmd.md",
  "specs/implemented/006_spec-tdd-example-minimal-api-efcore.md"
]
supersedes: []
source_purpose: "Refactor SeedService and configuration bootstrapping so environment variables, OS variables, and user secrets are NEVER saved to the database table; only settings from explicit appsettings.json file sources are seeded into SQL storage."
---

# Specification: Environment Variable & Secret Exclusion from Database Seeding

## 1. Purpose & Scope

### 1.1 Problem Statement

Currently, when `SeedService` runs against a bootstrap `IConfiguration` (or `IConfigurationRoot`) that includes `AddEnvironmentVariables()`, all operating system environment variables (such as `PATH`, `HOME`, `USER`, `ASPNETCORE_ENVIRONMENT`, system tokens) and process environment variables are automatically enumerated and persisted into the `AppSettings` database table.

Environment variables and secrets must **NEVER** be persisted to the database. They must continue to be accessible in memory for application configuration lookups and overrides, but the database seeding process must strictly restrict seeding to settings originating from `appsettings.json` (or `FileConfigurationProvider` / `JsonConfigurationProvider`).

### 1.2 In-Scope

- **Provider-Aware Seeding in `SeedService`**:
  - When `IConfiguration` is passed as `IConfigurationRoot`, `SeedService` inspects providers and extracts only keys originating from `JsonConfigurationProvider` (or file-based providers).
  - Providers derived from `EnvironmentVariablesConfigurationProvider`, `CommandLineConfigurationProvider`, or `MemoryConfigurationProvider` holding environment variables must be excluded from database persistence.
- **Direct JSON File Seeding Support**:
  - `SeedService` and `DbAppSettingsOptions` support specifying explicit JSON file paths (e.g., `appsettings.json`, `appsettings.{Environment}.json`) or stream sources for clean, deterministic seeding.
- **Environment Variable Filtering Safeguards**:
  - Explicit filter rejecting known OS variables and keys matching environment variable patterns during seeding.
- **Preservation of Runtime Configuration Layering**:
  - Environment variables registered in `IConfigurationBuilder` continue to be available to `IConfiguration` and `IOptions<T>` in memory (and override DB values according to standard .NET configuration order), but are omitted from DB storage.
- **Unit and Integration Tests**:
  - Verifying that when `bootstrapConfig` contains environment variables, secrets, and `appsettings.json`, ONLY the `appsettings.json` keys are inserted into the database.
  - Verifying that existing environment variables remain readable in `IConfiguration` at runtime.

### 1.3 Out-of-Scope

- Encryption of existing DB entries (already handled by `AesGcmValueEncryptor`).
- Changes to storage engine DDL schemas.

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `SeedService` | Service responsible for populating the database table on initial startup when the table is empty. |
| `JsonConfigurationProvider` | Microsoft Configuration provider that reads key-value pairs from physical JSON files. |
| `EnvironmentVariablesConfigurationProvider` | Microsoft Configuration provider that reads OS and process environment variables. |
| `Configuration Layering` | .NET convention where configuration sources are evaluated in order (`appsettings.json` $\rightarrow$ DB $\rightarrow$ Env Vars $\rightarrow$ CLI). |

---

## 3. Requirements & Constraints

- **REQ-001**: `SeedService` MUST NOT persist any keys originating from `EnvironmentVariablesConfigurationProvider` into the database.
- **REQ-002**: `SeedService` MUST NOT persist any keys originating from `CommandLineConfigurationProvider` into the database.
- **REQ-003**: When seeding from an `IConfigurationRoot`, `SeedService` MUST isolate keys provided by `JsonConfigurationProvider` / `FileConfigurationProvider`.
- **REQ-004**: If `IConfiguration` does not expose underlying providers (non-root wrapper), `SeedService` MUST filter out keys matching system environment variables retrieved via `System.Environment.GetEnvironmentVariables()`.
- **REQ-005**: Environment variables MUST remain readable in `IConfiguration` and `IOptions<T>` at application runtime.
- **REQ-006**: Bootstrap database connection strings (e.g., `DbAppSettings:ConnectionString`, `DbAppSettings__ConnectionString`) MUST continue to be excluded from seeding.

---

## 4. Architecture & Data Flow

```mermaid
flowchart TD
    subgraph Bootstrap["Bootstrap Configuration Sources"]
        JSON["appsettings.json (JsonConfigurationProvider)"]
        ENV["Environment Variables (EnvironmentVariablesConfigurationProvider)"]
        CMD["Command Line Args"]
    end

    subgraph Memory["In-Memory IConfiguration"]
        Config["IConfiguration / IConfigurationRoot"]
    end

    subgraph Seeding["SeedService (Filtered)"]
        Filter{"Is Key from JsonConfigurationProvider\nand NOT Environment Variable?"}
        DBEngine["IDbAppSettingsStorageEngine"]
    end

    subgraph Storage["Database Table"]
        Table[("AppSettings SQL Table\n(JSON keys ONLY)")]
    end

    JSON --> Config
    ENV --> Config
    CMD --> Config

    Config --> Filter
    Filter -->|YES (JSON Setting)| DBEngine
    Filter -->|NO (Env Var / OS Var / Secret)| Drop["Omit from DB Persist"]
    DBEngine --> Table
```

### 4.1 Seeding Key Extraction Logic

```csharp
public static IEnumerable<KeyValuePair<string, string?>> ExtractSeedableEntries(
    IConfiguration configuration,
    IReadOnlyList<string> excludedKeys)
{
    var excludeSet = new HashSet<string>(excludedKeys, StringComparer.OrdinalIgnoreCase);

    // 1. If IConfigurationRoot, query only Json/File providers
    if (configuration is IConfigurationRoot root)
    {
        var jsonProviders = root.Providers
            .Where(p => p is JsonConfigurationProvider or FileConfigurationProvider)
            .ToList();

        if (jsonProviders.Count > 0)
        {
            var seedData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var provider in jsonProviders)
            {
                foreach (var (key, value) in ExtractFromProvider(provider))
                {
                    if (!excludeSet.Contains(key) && !IsKnownBootstrapKey(key))
                    {
                        seedData[key] = value;
                    }
                }
            }
            return seedData;
        }
    }

    // 2. Fallback: Filter out all process and system environment variables
    var envVars = System.Environment.GetEnvironmentVariables()
        .Keys
        .Cast<object>()
        .Select(k => k.ToString()!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    return configuration.AsEnumerable(makePathsRelative: false)
        .Where(kvp => kvp.Value is not null)
        .Where(kvp => !excludeSet.Contains(kvp.Key))
        .Where(kvp => !IsKnownBootstrapKey(kvp.Key))
        .Where(kvp => !envVars.Contains(kvp.Key) && !envVars.Contains(kvp.Key.Split(':')[0]));
}
```

---

## 5. Dependencies & Integrations

- **`Microsoft.Extensions.Configuration.Json`**: For type inspection of `JsonConfigurationProvider` and `FileConfigurationProvider`.
- **`Microsoft.Extensions.Configuration.EnvironmentVariables`**: For explicit identification and exclusion of `EnvironmentVariablesConfigurationProvider`.

---

## 6. Acceptance Criteria

- **AC-001 (Environment Variables Excluded from Seeding)**:
  - **Given**: A bootstrap configuration built from `appsettings.json` (`"Application:Title": "My App"`) and `AddEnvironmentVariables()` (with environment variable `MY_SECRET_KEY="12345"` and OS `PATH`).
  - **When**: `SeedService.SeedAsync(bootstrapConfig)` executes on an empty database.
  - **Then**: The database table contains `"Application__Title"`, but contains **0** environment variable records (neither `MY_SECRET_KEY` nor `PATH`).
  - **[PHASE: RED] Failure Mode**: Database contains records for `MY_SECRET_KEY` or `PATH`.

- **AC-002 (Runtime Accessibility of Environment Variables)**:
  - **Given**: An application configured with `AddDbAppSettings` and environment variable `CUSTOM_ENV_VAR="active"`.
  - **When**: Querying `IConfiguration["CUSTOM_ENV_VAR"]` at runtime.
  - **Then**: The value returns `"active"`, while `IDbAppSettingsStorageEngine.GetAllAsync()` does NOT contain `CUSTOM_ENV_VAR`.
  - **[PHASE: RED] Failure Mode**: `IConfiguration` returns null or database contains the key.

- **AC-003 (Example Minimal API Clean SQLite Verification)**:
  - **Given**: The `examples/PH.DbAppSettings.Example.MinimalApi` project.
  - **When**: The Minimal API runs and seeds `App_Data/appsettings.db`.
  - **Then**: Inspecting `appsettings.db` records reveals strictly the `Application:*` settings and 0 environment variables.
  - **[PHASE: RED] Failure Mode**: `appsettings.db` contains environment variable rows.

---

## 7. Test Automation Strategy

### 7.1 Test Framework

- **xUnit 2.x/3.x** in `tests/PH.DbAppSettings.Tests`.
- Dedicated unit test fixture: `tests/PH.DbAppSettings.Tests/EnvironmentVariableExclusionTests.cs`.

### 7.2 Commands

```bash
# Run specific environment exclusion tests (RED / GREEN)
dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~EnvironmentVariableExclusionTests

# Run full solution test suite (REFACTOR)
dotnet test PH.DbAppSettings.slnx
```

---

## 8. Examples & Edge Cases

### 8.1 Edge Cases Covered

1. **Composite `IConfigurationRoot` with multiple JSON files**:
   - `appsettings.json` + `appsettings.Production.json` $\rightarrow$ Both JSON providers are correctly included in seed.
2. **Environment Variable overrides a JSON key**:
   - `appsettings.json` has `Application:Title="File Title"`, Environment Variable has `Application__Title="Env Override"`.
   - Database receives `"File Title"` from JSON source.
   - At runtime, `IConfiguration["Application:Title"]` returns `"Env Override"` because Environment Variables are layered on top of the DB provider in ASP.NET Core.
3. **OS-specific environment variables**:
   - Variables like `PATH`, `HOME`, `USER`, `HOSTNAME`, `DOTNET_*`, `TERM` are never seeded into the table.

---

## 9. Spec Validation & AI-Readiness

- [X] Self-contained context with explicit filtering algorithms.
- [X] Testable acceptance criteria with Given/When/Then and RED failure modes.
- [X] Explicit TDD tasks with RED $\rightarrow$ GREEN $\rightarrow$ REFACTOR triads.
- [X] Exact project paths and naming conventions defined.

---

## 10. References & Instructions

- `.agents/rules/csharp-coding-standards.md`
- `.agents/rules/security-secrets-policy.md`
- `.agents/skills/test-driven-development/SKILL.md`

---

## 11. Task Breakdown

```yaml
tasks:
  - id: TASK-014-RED
    title: "Write failing unit tests for Environment Variable exclusion during seeding"
    type: test
    priority: critical
    phase: RED
    objective: "Create tests/PH.DbAppSettings.Tests/EnvironmentVariableExclusionTests.cs verifying that environment variables and OS variables in bootstrap configuration are never seeded into database storage."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/EnvironmentVariableExclusionTests.cs"
        reason: "Test fixture asserting strict exclusion of environment variables from database seeding."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~EnvironmentVariableExclusionTests"
      - Expected: "Fails (RED) because current SeedService seeds all configuration entries including environment variables."

  - id: TASK-014-GREEN
    title: "Refactor SeedService to filter and exclude Environment Variables and non-file providers"
    type: code
    priority: critical
    phase: GREEN
    objective: "Update SeedService.cs to extract keys exclusively from JsonConfigurationProvider / FileConfigurationProvider when IConfigurationRoot is available, and filter out process/OS environment variables."
    files_to_modify:
      - path: "src/PH.DbAppSettings/Services/SeedService.cs"
        reason: "Implement provider-aware key extraction and environment variable filtering."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~EnvironmentVariableExclusionTests"
      - Expected: "Passes (GREEN)."

  - id: TASK-014-REFACTOR
    title: "Verify Example Minimal API and full solution test suite"
    type: refactor
    priority: high
    phase: REFACTOR
    objective: "Re-run full solution tests and verify that Example Minimal API seeds clean database without any environment variables."
    files_to_modify:
      - path: "tests/PH.DbAppSettings.Tests/ExampleMinimalApiTests.cs"
        reason: "Add assertion ensuring 0 environment variables exist in seeded SQLite DB."
    validation:
      - Run: "dotnet test PH.DbAppSettings.slnx"
      - Expected: "All tests pass (100% green)."
```

---

## 12. Conflict Detection

- **Conflict Check**: Progressive spec ID `007`. Specs 001-006 completed in `/specs/implemented/`.
- **Resolution**: No conflicts.

---

## 13. Files Added to Context

- `src/PH.DbAppSettings/Services/SeedService.cs`
- `src/PH.DbAppSettings/Configuration/DbAppSettingsProvider.cs`
- `examples/PH.DbAppSettings.Example.MinimalApi/Program.cs`
