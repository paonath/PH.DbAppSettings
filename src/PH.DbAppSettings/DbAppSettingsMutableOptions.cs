using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings;

/// <summary>
/// Versione mutabile di DbAppSettingsOptions, usata come target per Action&lt;T&gt; nei metodi di configurazione.
/// </summary>
public sealed class DbAppSettingsMutableOptions
{
    public string? ConnectionString { get; set; }
    public string Environment { get; set; } = "Production";
    public bool AutoMigrate { get; set; } = true;
    public bool SeedOnEmpty { get; set; } = true;
    public bool ForceReseed { get; set; } = false;
    public IReadOnlyList<string> ExcludeKeysFromSeed { get; set; } = [];
    public bool EncryptValues { get; set; } = false;
    public TimeSpan? ReloadInterval { get; set; } = null;
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = "AppSettings";
    public Func<IDbAppSettingsStorageEngine>? StorageEngineFactory { get; set; }

    /// <summary>
    /// Configura l'uso del motore Dapper con una connection factory e dialetto SQL.
    /// </summary>
    public void UseDapper(Func<DbConnection> connectionFactory, ISqlDialect? dialect = null)
    {
        StorageEngineFactory = () => new DapperStorageEngine(
            connectionFactory,
            dialect ?? new SqlServerDialect(),
            SchemaName,
            TableName);
    }

    /// <summary>
    /// Configura l'uso del motore Dapper per SQLite con connection string.
    /// </summary>
    public void UseDapperSqlite(string connectionString)
    {
        ConnectionString = connectionString;
        UseDapper(() => new SqliteConnection(connectionString), new SqliteDialect());
    }

    /// <summary>
    /// Configura l'uso del motore Entity Framework Core.
    /// </summary>
    public void UseEntityFramework(Action<DbContextOptionsBuilder<AppSettingsDbContext>> configureDbContext)
    {
        StorageEngineFactory = () =>
        {
            var builder = new DbContextOptionsBuilder<AppSettingsDbContext>();
            configureDbContext(builder);
            return new EfCoreStorageEngine(() => new AppSettingsDbContext(builder.Options));
        };
    }

    /// <summary>
    /// Configura l'uso del motore Entity Framework Core con SQLite.
    /// </summary>
    public void UseEntityFrameworkSqlite(string connectionString)
    {
        ConnectionString = connectionString;
        UseEntityFramework(builder => builder.UseSqlite(connectionString));
    }

    public DbAppSettingsOptions ToOptions()
    {
        if (StorageEngineFactory is null && string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("DbAppSettingsMutableOptions.ConnectionString or a storage engine configuration is required.");

        return new DbAppSettingsOptions
        {
            ConnectionString = ConnectionString,
            Environment = Environment,
            AutoMigrate = AutoMigrate,
            SeedOnEmpty = SeedOnEmpty,
            ForceReseed = ForceReseed,
            ExcludeKeysFromSeed = ExcludeKeysFromSeed,
            EncryptValues = EncryptValues,
            ReloadInterval = ReloadInterval,
            SchemaName = SchemaName,
            TableName = TableName,
            StorageEngineFactory = StorageEngineFactory
        };
    }
}
