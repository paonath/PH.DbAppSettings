---
title: "Table of Contents"
reasoning_path: "reasoning/unified-cli-in-core-package"
status: "completed"
created_at: "2026-08-24T14:50:23+02:00"
---

# Table of Contents: Unifying CLI Tooling into a Single PH.DbAppSettings Package

## Reasoning Steps

- `reasoning/unified-cli-in-core-package/steps/000-plan.md`
- `reasoning/unified-cli-in-core-package/steps/001-nuget-and-dotnet-tool-mechanics.md`
- `reasoning/unified-cli-in-core-package/steps/002-dependency-and-assembly-footprint-analysis.md`
- `reasoning/unified-cli-in-core-package/steps/003-cli-invocation-models-in-consumer-projects.md`
- `reasoning/unified-cli-in-core-package/steps/004-architectural-options-and-swot-matrix.md`
- `reasoning/unified-cli-in-core-package/steps/qa-midpoint.md`
- `reasoning/unified-cli-in-core-package/steps/005-single-package-dual-payload-verification.md`
- `reasoning/unified-cli-in-core-package/steps/006-target-architectural-blueprint.md`
- `reasoning/unified-cli-in-core-package/steps/007-implementation-and-migration-roadmap.md`
- `reasoning/unified-cli-in-core-package/steps/008-synthesis-preparation.md`

## Summary Document

- `reasoning/unified-cli-in-core-package/README.md`

## Attachments

### Workspace References

- `src/PH.DbAppSettings/PH.DbAppSettings.csproj`
- `src/PH.DbAppSettings/DbAppSettingsExtensions.cs`
- `src/PH.DbAppSettings/DbAppSettingsOptions.cs`
- `src/PH.DbAppSettings/DbAppSettingsMutableOptions.cs`
- `src/PH.DbAppSettings.Cli/PH.DbAppSettings.Cli.csproj`
- `src/PH.DbAppSettings.Cli/Program.cs`
- `src/PH.DbAppSettings.Cli/Commands/AnalyzeCommand.cs`
- `src/PH.DbAppSettings.Cli/Commands/ImportCommand.cs`
- `src/PH.DbAppSettings.Cli/Commands/IngestCommand.cs`
- `src/PH.DbAppSettings.Cli/Commands/ExportCommand.cs`
- `src/PH.DbAppSettings.Cli/Commands/RewriteJsonCommand.cs`
- `src/PH.DbAppSettings.Cli/Services/AppSettingsJsonAnalyzer.cs`
- `src/PH.DbAppSettings.Cli/Services/JsonTreeReconstructor.cs`
- `src/PH.DbAppSettings.Cli/Services/StorageEngineFactory.cs`
- `README.md`
- `AGENTS.md`
