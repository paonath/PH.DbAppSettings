---
title: "Specification: CLI Tool for appsettings.json Analysis, Import, and Export"
version: "1.0.0"
date_created: "2026-08-21 10:44:00"
last_updated: "2026-08-21 10:44:00"
tags: [tdd, dotnet, cli, json-import, json-analysis]
git_commit: ""
git_branch: "main"
status: completed
related_specs: ["specs/001_spec-tdd-key-normalization-and-options-binding.md", "specs/002_spec-tdd-storage-abstraction-and-multi-dialect-dapper.md", "specs/003_spec-tdd-efcore-modernization-and-reload-optimization.md"]
supersedes: []
source_purpose: "Create a CLI tool (PH.DbAppSettings.Cli) to analyze appsettings.json, detect sensitive keys, import entries into database tables across supported dialects, and export database configuration back to JSON."
---

# Specification: CLI Tool for appsettings.json Analysis, Import, and Export

## 1. Purpose & Scope

### 1.1 Problem Statement

Operators and developers need a command-line utility to inspect existing `appsettings.json` files, identify sensitive keys, and seed or import configuration directly into SQL databases without writing manual SQL insert scripts.

### 1.2 In-Scope

- Creation of `PH.DbAppSettings.Cli` console project in `src/PH.DbAppSettings.Cli/`.
- `analyze` command: parses `appsettings.json`, prints formatted key hierarchy table, detects array indexes, and flags sensitive keys (e.g. passwords, secrets, tokens).
- `import` command: parses `appsettings.json`, creates table idempotently if missing, optionally encrypts sensitive keys using AES-GCM, and upserts entries into the database.
- `export` command: reads database records for an environment and reconstructs structured JSON.
- TDD unit and integration tests for all CLI commands.

### 1.3 Out-of-Scope

- Web-based GUI dashboard.

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `AnalyzeCommand` | Inspects JSON file and outputs key inventory with secret heuristics. |
| `ImportCommand` | Seeds or imports JSON settings into target database table. |
| `ExportCommand` | Exports database configuration to JSON file. |

---

## 3. Requirements & Constraints

- **REQ-001**: `analyze` command MUST parse hierarchical JSON files and display all flattened keys, data types, and secret flags.
- **REQ-002**: `import` command MUST support parameters `--file`, `--connection-string`, `--engine` (dapper/efcore), `--dialect` (sqlserver/postgres/sqlite/mysql), `--environment`, and optional `--encrypt`.
- **REQ-003**: `import` command MUST create the database table idempotently on startup before inserting records.
- **REQ-004**: `export` command MUST reconstruct valid JSON hierarchy matching the original configuration format.
- **SEC-001**: CLI tool MUST NOT print plaintext decrypted secrets in console outputs unless explicitly forced with a debug flag.

---

## 4. Architecture & Interfaces

### 4.1 CLI Command Line Interface

```bash
# Analyze command
dotnet dbappsettings analyze <path-to-json>

# Import command
dotnet dbappsettings import <path-to-json> \
  --connection-string <conn> \
  --engine <dapper|efcore> \
  --dialect <sqlserver|postgres|sqlite|mysql> \
  --environment <env> \
  [--encrypt] \
  [--secret <encryption-secret>]

# Export command
dotnet dbappsettings export \
  --connection-string <conn> \
  --engine <dapper|efcore> \
  --dialect <sqlserver|postgres|sqlite|mysql> \
  --environment <env> \
  [--output <path-to-output-json>]
```

---

## 5. Dependencies & Integrations

- `PH.DbAppSettings` (Project Reference)
- `System.CommandLine` or `Spectre.Console`
- `System.Text.Json`

---

## 6. Acceptance Criteria

- **AC-001**:
  - **Given**: A sample `appsettings.json` containing `ConnectionStrings:Default` and `Logging:LogLevel:Default`.
  - **When**: `AnalyzeCommand` is executed on the file.
  - **Then**: Discovers all keys, correctly flags `ConnectionStrings:Default` as sensitive, and exits with code 0.
  - **RED Failure Mode**: Execution throws error or fails to discover nested keys.

- **AC-002**:
  - **Given**: An empty SQLite database and valid `appsettings.json`.
  - **When**: `ImportCommand` is executed with `--engine dapper --dialect sqlite`.
  - **Then**: `AppSettings` table is created and all records are populated in database.
  - **RED Failure Mode**: Command fails with missing table or insertion error.

- **AC-003**:
  - **Given**: A populated database.
  - **When**: `ExportCommand` is executed.
  - **Then**: Outputs valid, formatted JSON reconstructing the original hierarchical structure.
  - **RED Failure Mode**: Output is invalid JSON or malformed flat dictionary.

---

## 7. Test Automation Strategy

### 7.1 Test Execution Commands

- **CLI Unit Tests (RED/GREEN)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~CliCommandTests`
- **Full Suite Verification (REFACTOR)**:
  `dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj`

---

## 8. Examples & Edge Cases

### 8.1 Sensitive Key Heuristics

The CLI flags keys containing: `password`, `secret`, `key`, `token`, `connectionstring`, `apikey`, `pwd`, `credential`.

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
  - id: TASK-007-RED
    title: "Write Failing Unit Tests for AnalyzeCommand"
    type: test
    phase: RED
    priority: high
    objective: "Author CliCommandTests verifying JSON tree analysis, key flattening, and sensitive key detection."
    acceptance_criteria:
      - "AC: Test fails due to missing AnalyzeCommand."
    files_to_create:
      - path: "tests/PH.DbAppSettings.Tests/CliCommandTests.cs"
        reason: "TDD test fixture for CLI commands."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~CliCommandTests"

  - id: TASK-007-GREEN
    title: "Implement PH.DbAppSettings.Cli and AnalyzeCommand"
    type: code
    phase: GREEN
    priority: high
    objective: "Create src/PH.DbAppSettings.Cli project and implement AnalyzeCommand."
    acceptance_criteria:
      - "AC: CliCommandTests passes analyze test cases."
    files_to_create:
      - path: "src/PH.DbAppSettings.Cli/PH.DbAppSettings.Cli.csproj"
        reason: "CLI project file."
      - path: "src/PH.DbAppSettings.Cli/Program.cs"
        reason: "CLI entry point."
      - path: "src/PH.DbAppSettings.Cli/Commands/AnalyzeCommand.cs"
        reason: "Analyze command implementation."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~CliCommandTests"

  - id: TASK-007-REFACTOR
    title: "Refactor AnalyzeCommand"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Clean up console formatting and table rendering."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings.Cli/Commands/AnalyzeCommand.cs"
        reason: "Output formatting optimization."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"

  - id: TASK-008-RED
    title: "Write Failing Unit Tests for ImportCommand and ExportCommand"
    type: test
    phase: RED
    priority: high
    objective: "Add tests in CliCommandTests for database import and JSON export."
    acceptance_criteria:
      - "AC: Test fails due to missing ImportCommand and ExportCommand."
    files_to_modify:
      - path: "tests/PH.DbAppSettings.Tests/CliCommandTests.cs"
        reason: "Add import and export test cases."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~CliCommandTests"

  - id: TASK-008-GREEN
    title: "Implement ImportCommand and ExportCommand"
    type: code
    phase: GREEN
    priority: high
    objective: "Implement ImportCommand and ExportCommand in PH.DbAppSettings.Cli."
    acceptance_criteria:
      - "AC: All CLI tests turn green."
    files_to_create:
      - path: "src/PH.DbAppSettings.Cli/Commands/ImportCommand.cs"
        reason: "Import command implementation."
      - path: "src/PH.DbAppSettings.Cli/Commands/ExportCommand.cs"
        reason: "Export command implementation."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj --filter FullyQualifiedName~CliCommandTests"

  - id: TASK-008-REFACTOR
    title: "Refactor CLI Commands"
    type: refactor
    phase: REFACTOR
    priority: low
    objective: "Unify error handling and parameter parsing across all commands."
    acceptance_criteria:
      - "AC: Full test suite remains green."
    files_to_modify:
      - path: "src/PH.DbAppSettings.Cli/Program.cs"
        reason: "Command line parser cleanup."
    validation:
      - Run: "dotnet test tests/PH.DbAppSettings.Tests/PH.DbAppSettings.Tests.csproj"
```

---

## 12. Conflict Detection

- **Conflict Analysis**: Adds a dedicated console project without interfering with the core library.
- **Resolution**: Add `PH.DbAppSettings.Cli` to `PH.DbAppSettings.slnx`.

---

## 13. Files Added to Context

- `src/PH.DbAppSettings.Cli/PH.DbAppSettings.Cli.csproj`
