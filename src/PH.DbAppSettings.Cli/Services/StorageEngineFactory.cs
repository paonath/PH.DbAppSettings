using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Cli.Services;

public static class StorageEngineFactory
{
    public static (IDbAppSettingsStorageEngine Engine, ISqlDialect Dialect) Create(
        string connectionString,
        string dialectName,
        string schema = "dbo",
        string table = "AppSettings")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var normalizedDialect = (dialectName ?? "sqlserver").Trim().ToLowerInvariant();

        return normalizedDialect switch
        {
            "sqlite" => (
                new DapperStorageEngine(() => new SqliteConnection(connectionString), new SqliteDialect(), schema, table),
                new SqliteDialect()),

            "postgres" or "postgresql" => (
                new DapperStorageEngine(() => new NpgsqlConnection(connectionString), new PostgreSqlDialect(), string.IsNullOrWhiteSpace(schema) ? "public" : schema, table),
                new PostgreSqlDialect()),

            "mysql" or "mariadb" => (
                new DapperStorageEngine(() => new MySqlConnection(connectionString), new MySqlDialect(), schema, table),
                new MySqlDialect()),

            "sqlserver" or "mssql" => (
                new DapperStorageEngine(() => new SqlConnection(connectionString), new SqlServerDialect(), string.IsNullOrWhiteSpace(schema) ? "dbo" : schema, table),
                new SqlServerDialect()),

            _ => throw new NotSupportedException($"Unsupported SQL dialect: '{dialectName}'. Supported: sqlserver, postgres, sqlite, mysql.")
        };
    }
}
