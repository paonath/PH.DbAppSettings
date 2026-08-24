using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class DbAppSettingsProviderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;

    public DbAppSettingsProviderTests()
    {
        // Use a named in-memory SQLite DB so the provider can open its own connection
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();

        // Ensure schema is created
        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var ctx = new TestAppSettingsDbContext(options);
        ctx.Database.EnsureCreated();
    }

    private void SeedDirectly(Dictionary<string, string?> entries, string env = "Test")
    {
        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var ctx = new TestAppSettingsDbContext(options);
        foreach (var (key, value) in entries)
        {
            ctx.AppSettings.Add(new AppSettingEntry { Key = key, Environment = env, Value = value });
        }
        ctx.SaveChanges();
    }

    [Fact]
    public void Load_PopulatesDataFromDb()
    {
        SeedDirectly(new Dictionary<string, string?>
        {
            ["MyApp__Setting1"] = "value1",
            ["MyApp__Setting2"] = "value2"
        });

        var opts = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            AutoMigrate = false,
            SeedOnEmpty = false,
            StorageEngineFactory = () => new EfCoreStorageEngine(() =>
            {
                var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
                    .UseSqlite(_connection)
                    .Options;
                return new TestAppSettingsDbContext(options);
            })
        };

        var provider = new DbAppSettingsProvider(opts);
        provider.Load();

        Assert.True(provider.TryGet("MyApp__Setting1", out var v1));
        Assert.Equal("value1", v1);
        Assert.True(provider.TryGet("MyApp__Setting2", out var v2));
        Assert.Equal("value2", v2);
    }

    [Fact]
    public void Load_OnlyLoadsCorrectEnvironment()
    {
        SeedDirectly(new Dictionary<string, string?> { ["Key1"] = "prod-value" }, "Production");
        SeedDirectly(new Dictionary<string, string?> { ["Key1"] = "test-value" }, "Test");

        var opts = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            AutoMigrate = false,
            SeedOnEmpty = false,
            StorageEngineFactory = () => new EfCoreStorageEngine(() =>
            {
                var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
                    .UseSqlite(_connection)
                    .Options;
                return new TestAppSettingsDbContext(options);
            })
        };

        var provider = new DbAppSettingsProvider(opts);
        provider.Load();

        Assert.True(provider.TryGet("Key1", out var v));
        Assert.Equal("test-value", v);
    }

    [Fact]
    public void Load_WithSeedOnEmpty_SeedsFromBootstrapConfig()
    {
        var bootstrapConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MyApp:Feature"] = "enabled"
            })
            .Build();

        var opts = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            AutoMigrate = false,
            SeedOnEmpty = true,
            StorageEngineFactory = () => new EfCoreStorageEngine(() =>
            {
                var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
                    .UseSqlite(_connection)
                    .Options;
                return new TestAppSettingsDbContext(options);
            })
        };

        var provider = new DbAppSettingsProvider(opts, bootstrapConfig);
        provider.Load();

        Assert.True(provider.TryGet("MyApp__Feature", out var v));
        Assert.Equal("enabled", v);
    }

    [Fact]
    public void Load_ThrowsInvalidOperationException_WhenNoStorageEngineConfigured()
    {
        var opts = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test"
        };

        var provider = new DbAppSettingsProvider(opts);
        var ex = Assert.Throws<InvalidOperationException>(() => provider.Load());
        Assert.Contains("No storage engine configured", ex.Message);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
