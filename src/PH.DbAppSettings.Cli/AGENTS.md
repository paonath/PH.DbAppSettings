# AGENTS.md - src/PH.DbAppSettings.Cli

## Project Scope

Global .NET tool CLI application (`dbappsettings`) for analyzing, inspecting, importing, and exporting `appsettings.json` configuration files with multi-dialect database storage.

## CLI Architecture & Commands

- **`Commands/`**:
  - [AnalyzeCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/AnalyzeCommand.cs): `analyze <FILE>` - Recursively flattens JSON settings, identifies nested structures, and flags sensitive properties (passwords, secrets, connection strings, keys).
  - [ImportCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/ImportCommand.cs): `import <FILE> -c <CONN> -d <DIALECT> -e <ENV>` - Parses and inserts JSON configuration items directly into the specified database table.
  - [ExportCommand](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Commands/ExportCommand.cs): `export -c <CONN> -d <DIALECT> -e <ENV> -o <OUT>` - Queries configuration records from database table and reconstructs structured, formatted JSON files.
- **`Services/`**:
  - [AppSettingsJsonAnalyzer](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Services/AppSettingsJsonAnalyzer.cs): Core recursive JSON traversal and sensitive key detection engine.
  - [StorageEngineFactory](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/src/PH.DbAppSettings.Cli/Services/StorageEngineFactory.cs): Factory that creates appropriate ADO.NET connection providers (`Microsoft.Data.SqlClient`, `Npgsql`, `Microsoft.Data.Sqlite`, `MySqlConnector`) and dialect strategies.

## Command Examples

```bash
# Analyze a configuration file
dotnet run --project src/PH.DbAppSettings.Cli -- analyze appsettings.json --detailed

# Import appsettings.json into a SQLite database
dotnet run --project src/PH.DbAppSettings.Cli -- import appsettings.json -c "Data Source=appsettings.db" -d sqlite -e Production

# Import with encryption for sensitive keys
dotnet run --project src/PH.DbAppSettings.Cli -- import appsettings.json -c "Server=localhost;Database=ConfigDb;User Id=sa;Password=secret;" -d sqlserver -e Production --encrypt-secret "my-32-character-secret-key-1234"

# Export database entries back to formatted JSON
dotnet run --project src/PH.DbAppSettings.Cli -- export -c "Data Source=appsettings.db" -d sqlite -e Production -o appsettings.Production.json
```
