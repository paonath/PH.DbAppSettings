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
    public bool UseMigrations { get; set; } = false;
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
    /// Configura l'uso del motore Entity Framework Core con un delegate factory.
    /// </summary>
    public void UseEntityFramework<TContext>(Func<TContext> contextFactory)
        where TContext : AppSettingsDbContext
    {
        StorageEngineFactory = () => new EfCoreStorageEngine(() => contextFactory(), UseMigrations);
    }

    /// <summary>
    /// Configura l'uso del motore Entity Framework Core configurando DbContextOptionsBuilder.
    /// </summary>
    public void UseEntityFramework<TContext>(Action<DbContextOptionsBuilder<TContext>> configureDbContext)
        where TContext : AppSettingsDbContext
    {
        StorageEngineFactory = () =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            configureDbContext(builder);
            return new EfCoreStorageEngine(() => (TContext)Activator.CreateInstance(typeof(TContext), builder.Options)!, UseMigrations);
        };
    }

    /// <summary>
    /// Configura l'uso del motore Entity Framework Core configurando DbContextOptionsBuilder con la ConnectionString risolta.
    /// </summary>
    public void UseEntityFramework<TContext>(Action<DbContextOptionsBuilder<TContext>, string> configureDbContext)
        where TContext : AppSettingsDbContext
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionString must be configured before calling UseEntityFramework with connectionString delegate.");
        }

        StorageEngineFactory = () =>
        {
            var builder = new DbContextOptionsBuilder<TContext>();
            configureDbContext(builder, ConnectionString);
            return new EfCoreStorageEngine(() => (TContext)Activator.CreateInstance(typeof(TContext), builder.Options)!, UseMigrations);
        };
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
            UseMigrations = UseMigrations,
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
