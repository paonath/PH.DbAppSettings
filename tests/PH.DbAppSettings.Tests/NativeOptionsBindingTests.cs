using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;

namespace PH.DbAppSettings.Tests;

public class NativeOptionsBindingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;

    public NativeOptionsBindingTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var ctx = new AppSettingsDbContext(options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }

    public record FeatureFlags
    {
        public bool EnableCache { get; init; }
        public bool EnableDarkMode { get; init; }
    }

    private void SeedDatabase(Dictionary<string, string?> entries, string environment = "Production")
    {
        var options = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var ctx = new AppSettingsDbContext(options);
        foreach (var (key, value) in entries)
        {
            ctx.AppSettings.Add(new AppSettingEntry
            {
                Key = key,
                Environment = environment,
                Value = value
            });
        }
        ctx.SaveChanges();
    }

    [Fact]
    public void Configure_PocoClassOptions_BindsFromDatabaseKeysWithDoubleUnderscore()
    {
        // Arrange: Database contains keys with double underscore
        SeedDatabase(new Dictionary<string, string?>
        {
            ["Smtp__Host"] = "smtp.example.com",
            ["Smtp__Port"] = "587",
            ["Smtp__UseSsl"] = "true"
        });

        var provider = new DbAppSettingsProvider(new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Production",
            AutoMigrate = false,
            SeedOnEmpty = false
        });
        provider.Load();

        var config = new ConfigurationBuilder()
            .Add(new SimpleConfigurationSource(provider))
            .Build();

        var services = new ServiceCollection();
        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<SmtpOptions>>().Value;

        // Assert: Native binding should populate properties correctly
        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(587, options.Port);
        Assert.True(options.UseSsl);
    }

    [Fact]
    public void Configure_PositionalRecord_BindsFromDatabaseKeysWithDoubleUnderscore()
    {
        // Arrange
        SeedDatabase(new Dictionary<string, string?>
        {
            ["Features__EnableCache"] = "true",
            ["Features__EnableDarkMode"] = "false"
        });

        var provider = new DbAppSettingsProvider(new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Production",
            AutoMigrate = false,
            SeedOnEmpty = false
        });
        provider.Load();

        var config = new ConfigurationBuilder()
            .Add(new SimpleConfigurationSource(provider))
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeatureFlags>(config.GetSection("Features"));
        var sp = services.BuildServiceProvider();

        // Act
        var options = sp.GetRequiredService<IOptions<FeatureFlags>>().Value;

        // Assert
        Assert.True(options.EnableCache);
        Assert.False(options.EnableDarkMode);
    }

    [Fact]
    public void IOptionsMonitor_UpdatesCurrentValue_WhenProviderReloads()
    {
        // Arrange
        SeedDatabase(new Dictionary<string, string?>
        {
            ["Features__EnableCache"] = "false",
            ["Features__EnableDarkMode"] = "false"
        });

        var provider = new DbAppSettingsProvider(new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Production",
            AutoMigrate = false,
            SeedOnEmpty = false
        });
        provider.Load();

        var config = new ConfigurationBuilder()
            .Add(new SimpleConfigurationSource(provider))
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeatureFlags>(config.GetSection("Features"));
        var sp = services.BuildServiceProvider();

        var monitor = sp.GetRequiredService<IOptionsMonitor<FeatureFlags>>();
        Assert.False(monitor.CurrentValue.EnableCache);

        // Act: Update configuration and trigger reload
        provider.Set("Features:EnableCache", "true");

        // Assert: Monitor should reflect the updated value
        Assert.True(monitor.CurrentValue.EnableCache);
    }

    private class SimpleConfigurationSource(IConfigurationProvider provider) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => provider;
    }
}
