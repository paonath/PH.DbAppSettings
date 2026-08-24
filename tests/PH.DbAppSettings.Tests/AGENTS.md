# AGENTS.md - tests/PH.DbAppSettings.Tests

## Project Scope

Test project containing unit tests and integration tests for `PH.DbAppSettings` and `PH.DbAppSettings.Cli`.

## Test Organization

- [KeyNormalizerTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/KeyNormalizerTests.cs): Tests bidirectional normalization between `:` and `__`.
- [NativeOptionsBindingTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/NativeOptionsBindingTests.cs): Tests Microsoft Options binding (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) from database keys.
- [SqlDialectTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/SqlDialectTests.cs): Tests SQL DDL and query generation across SQL Server, PostgreSQL, SQLite, and MySQL dialects.
- [DapperStorageEngineTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/DapperStorageEngineTests.cs): Tests Dapper storage engine CRUD and schema migration.
- [EfCoreStorageEngineTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/EfCoreStorageEngineTests.cs): Tests EF Core storage engine CRUD and timestamp tracking.
- [TimestampReloadTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/TimestampReloadTests.cs): Tests background polling and reload detection based on `MAX(UpdatedAt)`.
- [AppSettingsJsonAnalyzerTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/AppSettingsJsonAnalyzerTests.cs): Tests JSON flattening and sensitive key heuristics.
- [CliCommandTests](file:///Users/paoloinnocenti/Documents/git/PH.DbAppSettings/tests/PH.DbAppSettings.Tests/CliCommandTests.cs): Tests end-to-end CLI command execution (`analyze`, `import`, `export`).

## Testing Discipline

- **Strict TDD**: Write failing unit/integration tests first (`[PHASE: RED]`), implement minimal code to pass (`[PHASE: GREEN]`), and clean up code while keeping tests green (`[PHASE: REFACTOR]`).
- **Isolation**: Use SQLite in-memory connections with unique GUID names (`Data Source=<guid>;Mode=Memory;Cache=Shared`) for isolated, fast test execution without disk I/O.
- **Determinism**: Never use arbitrary `Thread.Sleep`. Use async polling loops with timeout assertions when testing background workers.
