using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class GenericExtensionsTests
{
    [Fact]
    public void UseEntityFramework_WithBuilderAction_ProducesValidStorageEngine()
    {
        // Arrange
        var mutable = new DbAppSettingsMutableOptions();
        mutable.UseEntityFramework<TestAppSettingsDbContext>(builder =>
            builder.UseSqlite("Data Source=:memory:"));

        // Act
        var options = mutable.ToOptions();

        // Assert
        Assert.NotNull(options.StorageEngineFactory);
        var engine = options.StorageEngineFactory();
        Assert.NotNull(engine);
        Assert.IsType<EfCoreStorageEngine>(engine);
    }

    [Fact]
    public void UseEntityFramework_WithConnectionStringDelegate_ProducesValidStorageEngine()
    {
        // Arrange
        var mutable = new DbAppSettingsMutableOptions
        {
            ConnectionString = "Data Source=:memory:"
        };
        mutable.UseEntityFramework<TestAppSettingsDbContext>((builder, connStr) =>
            builder.UseSqlite(connStr));

        // Act
        var options = mutable.ToOptions();

        // Assert
        Assert.NotNull(options.StorageEngineFactory);
        var engine = options.StorageEngineFactory();
        Assert.NotNull(engine);
        Assert.IsType<EfCoreStorageEngine>(engine);
    }

    [Fact]
    public void UseEntityFramework_WithConnectionStringDelegate_ThrowsWhenConnectionStringMissing()
    {
        // Arrange
        var mutable = new DbAppSettingsMutableOptions();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            mutable.UseEntityFramework<TestAppSettingsDbContext>((builder, connStr) =>
                builder.UseSqlite(connStr)));

        Assert.Contains("ConnectionString must be configured", ex.Message);
    }

    [Fact]
    public void AddDbAppSettings_GenericConfigurationSource_AddsProvider()
    {
        // Arrange
        var connString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        using var keepAliveConn = new SqliteConnection(connString);
        keepAliveConn.Open();

        var configBuilder = new ConfigurationBuilder();
        var bootstrapConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbAppSettings:ConnectionString"] = connString,
                ["ASPNETCORE_ENVIRONMENT"] = "Test"
            })
            .Build();

        // Act
        configBuilder.AddDbAppSettings<TestAppSettingsDbContext>(bootstrapConfig, options =>
        {
            options.UseEntityFramework<TestAppSettingsDbContext>((b, conn) => b.UseSqlite(conn));
            options.AutoMigrate = true;
            options.SeedOnEmpty = false;
        });

        var config = configBuilder.Build();

        // Assert
        var provider = config.Providers.OfType<DbAppSettingsProvider>().FirstOrDefault();
        Assert.NotNull(provider);
    }

    [Fact]
    public void AddDbAppSettingsServices_Generic_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DbAppSettings:ConnectionString"] = "Data Source=:memory:"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddDbAppSettingsServices<TestAppSettingsDbContext>(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.UseEntityFramework<TestAppSettingsDbContext>((b, conn) => b.UseSqlite(conn));
            options.ReloadInterval = TimeSpan.FromSeconds(10);
        });

        var sp = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(sp.GetService<DbAppSettingsOptions>());
        Assert.NotNull(sp.GetService<IDbAppSettingsStorageEngine>());
        Assert.NotNull(sp.GetService<IDbAppSettingsReader>());
        Assert.NotNull(sp.GetService<IDbAppSettingsWriter>());
        Assert.NotNull(sp.GetService<SeedService>());
        Assert.NotNull(sp.GetService<ReloadBackgroundService>());
    }
}
