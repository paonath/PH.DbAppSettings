# AGENTS.md - src/PH.DbAppSettings.Cli

## Project Scope

Global .NET tool CLI application (`dbappsettings`) for analyzing, inspecting, importing, and exporting `appsettings.json` configuration files with multi-dialect database storage.

## CLI Architecture & Commands

- **`Commands/`**:
  - [AnalyzeCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/AnalyzeCommand.cs): `analyze <FILE>` - Recursively flattens JSON settings, identifies nested structures, and flags sensitive properties (passwords, secrets, connection strings, keys).
  - [ImportCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/ImportCommand.cs): `import <FILE> -c <CONN> -d <DIALECT> -e <ENV>` - Parses and inserts JSON configuration items directly into the specified database table.
  - [IngestCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/IngestCommand.cs): `ingest <FILE> -c <CONN> -d <DIALECT> -e <ENV> [-y|--yes]` - Imports JSON settings into the database table and safely deletes the source file upon confirmation.
  - [ExportCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/ExportCommand.cs): `export -c <CONN> -d <DIALECT> -e <ENV> -o <OUT>` - Queries configuration records from database table and reconstructs structured, formatted JSON files.
  - [RewriteJsonCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/RewriteJsonCommand.cs): `rewrite-json -c <CONN> -d <DIALECT> -e <ENV> [-o <OUT>]` - Reconstructs a fully typed and array-structured `appsettings.json` configuration file from the database.
- **`Services/`**:
  - [AppSettingsJsonAnalyzer](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Services/AppSettingsJsonAnalyzer.cs): Core recursive JSON traversal and sensitive key detection engine.
  - [JsonTreeReconstructor](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Services/JsonTreeReconstructor.cs): Reconstructs typed JSON trees and sequential arrays from database key-value pairs.
  - [StorageEngineFactory](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Services/StorageEngineFactory.cs): Factory that creates appropriate ADO.NET connection providers (`Microsoft.Data.SqlClient`, `Npgsql`, `Microsoft.Data.Sqlite`, `MySqlConnector`) and dialect strategies.

## Command Examples

```bash
# Analyze a configuration file
dotnet run --project src/PH.DbAppSettings.Cli -- analyze appsettings.json --detailed

# Import appsettings.json into a SQLite database
dotnet run --project src/PH.DbAppSettings.Cli -- import appsettings.json -c "Data Source=appsettings.db" -d sqlite -e Production

# Ingest and delete source appsettings.json
dotnet run --project src/PH.DbAppSettings.Cli -- ingest appsettings.json -c "Data Source=appsettings.db" -d sqlite -e Production -y

# Reconstruct / rewrite appsettings.json from database table
dotnet run --project src/PH.DbAppSettings.Cli -- rewrite-json -c "Data Source=appsettings.db" -d sqlite -e Production -o appsettings.json

# Export database entries back to formatted JSON
dotnet run --project src/PH.DbAppSettings.Cli -- export -c "Data Source=appsettings.db" -d sqlite -e Production -o appsettings.Production.json
```
