---
step: "007"
title: "Implementation and Migration Roadmap"
status: "completed"
created_at: "2026-08-24T15:05:20+02:00"
---

# Implementation and Migration Roadmap: Unifying CLI into PH.DbAppSettings

## Purpose

Define the phased implementation sequence for transitioning all CLI capabilities into `PH.DbAppSettings`, decommissioning the secondary project `PH.DbAppSettings.Cli`, and verifying full test coverage via TDD.

---

## Phased Implementation Sequence

### Phase 1: Port CLI Business Services to Core Assembly
- Move `AppSettingsJsonAnalyzer.cs`, `JsonTreeReconstructor.cs`, `FlattenedSettingItem.cs`, and `AppSettingsAnalysisResult.cs` from `PH.DbAppSettings.Cli` to `PH.DbAppSettings/Cli/`.
- Adjust namespaces to `PH.DbAppSettings.Cli` within `PH.DbAppSettings`.
- Verify that analyzers and JSON tree reconstruction operate with zero external dependencies (using `System.Text.Json`).

### Phase 2: Implement Embedded In-App CLI Runner
- Implement `DbAppSettingsCliRunner` in `PH.DbAppSettings/Cli/DbAppSettingsCliRunner.cs` supporting subcommands:
  - `analyze <file>`: Analyzes JSON and outputs sensitive keys/types.
  - `import <file> [-e <env>]`: Imports JSON settings into the configured database table.
  - `ingest <file> [-e <env>] [-y]`: Imports settings and deletes source JSON file.
  - `export <output> [-e <env>]`: Exports settings from database to JSON file.
  - `rewrite-json <output> [-e <env>]`: Reconstructs typed JSON tree from database.
- Implement extension method `public static bool RunDbAppSettingsCli(this IHost host, string[] args)` in `DbAppSettingsExtensions.cs`.
- Implement `public static bool RunDbAppSettingsCli(this IServiceProvider serviceProvider, string[] args)`.

### Phase 3: Add MSBuild Targets
- Create `src/PH.DbAppSettings/build/PH.DbAppSettings.targets`.
- Configure `PH.DbAppSettings.csproj` to include `build/` in NuGet packaging.

### Phase 4: Migrate Tests & Decommission `PH.DbAppSettings.Cli`
- Migrate unit tests from `CliCommandTests.cs` and `AppSettingsJsonAnalyzerTests.cs` to test against the embedded `DbAppSettingsCliRunner` in `PH.DbAppSettings.Tests`.
- Remove `PH.DbAppSettings.Cli` project from solution `PH.DbAppSettings.slnx` and delete `src/PH.DbAppSettings.Cli/`.
- Update `examples/PH.DbAppSettings.Example.MinimalApi/Program.cs` to demonstrate `if (app.RunDbAppSettingsCli(args)) return;`.

### Phase 5: Documentation & Packaging Validation
- Update `README.md`, `AGENTS.md`, and `CHANGELOG.md`.
- Run `dotnet test PH.DbAppSettings.slnx` and `dotnet format PH.DbAppSettings.slnx`.
- Verify `dotnet pack src/PH.DbAppSettings/PH.DbAppSettings.csproj` produces the single unified package.
