using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class TypedReadingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;
    private readonly TestAppSettingsDbContext _dbContext;

    public TypedReadingTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new TestAppSettingsDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }

    public record FeatureFlags(bool EnableCache, bool EnableDarkMode);

    [Fact]
    public void Test_Get_PocoClass_ReturnsPopulatedObject()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "smtp.example.com",
                ["Smtp:Port"] = "587",
                ["Smtp:UseSsl"] = "true"
            })
            .Build();

        var reader = new DbAppSettingsReader(config);
        var options = reader.Get<SmtpOptions>("Smtp");

        Assert.NotNull(options);
        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(587, options.Port);
        Assert.True(options.UseSsl);
    }

    [Fact]
    public void Test_Get_PositionalRecord_ReturnsPopulatedRecord()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:EnableCache"] = "true",
                ["Features:EnableDarkMode"] = "false"
            })
            .Build();

        var reader = new DbAppSettingsReader(config);
        var flags = reader.Get<FeatureFlags>("Features");

        Assert.NotNull(flags);
        Assert.True(flags.EnableCache);
        Assert.False(flags.EnableDarkMode);
    }

    [Fact]
    public void Test_GetValue_Int_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MaxRetries"] = "5" })
            .Build();

        var reader = new DbAppSettingsReader(config);
        var value = reader.GetValue<int>("MaxRetries");

        Assert.Equal(5, value);
    }

    [Fact]
    public void Test_GetValue_Bool_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["IsActive"] = "True" })
            .Build();

        var reader = new DbAppSettingsReader(config);
        var value = reader.GetValue<bool>("IsActive");

        Assert.True(value);
    }

    [Fact]
    public void Test_GetValue_MissingKey_ReturnsDefault()
    {
        var config = new ConfigurationBuilder().Build();
        var reader = new DbAppSettingsReader(config);

        var intValue = reader.GetValue("Missing", 42);
        var strValue = reader.GetValue("Missing", "default");

        Assert.Equal(42, intValue);
        Assert.Equal("default", strValue);
    }

    [Fact]
    public void Test_Get_WithDoubleUnderscoreKey_NormalizesCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:Smtp:Host"] = "smtp.test.com"
            })
            .Build();

        var reader = new DbAppSettingsReader(config);

        // Simulates a section key with double underscore
        var options = reader.Get<SmtpOptions>("App__Smtp");

        Assert.NotNull(options);
        Assert.Equal("smtp.test.com", options.Host);
    }

    [Fact]
    public async Task Test_SetAsyncTyped_Int_RoundTrip()
    {
        var dbOpts = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            StorageEngineFactory = () => new EfCoreStorageEngine(_dbContext)
        };
        var writer = new DbAppSettingsWriter(new EfCoreStorageEngine(_dbContext), dbOpts, NullLogger<DbAppSettingsWriter>.Instance);

        await writer.SetAsync<int>("Settings__MaxItems", 100);

        var provider = new DbAppSettingsProvider(new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            AutoMigrate = false,
            SeedOnEmpty = false,
            StorageEngineFactory = () => new EfCoreStorageEngine(_dbContext)
        });
        provider.Load();

        var config = new ConfigurationBuilder().Add(new TestProviderSource(provider)).Build();
        var reader = new DbAppSettingsReader(config);

        var readValue = reader.GetValue<int>("Settings__MaxItems");
        Assert.Equal(100, readValue);
    }

    [Fact]
    public async Task Test_SetAsyncTyped_Bool_RoundTrip()
    {
        var dbOpts = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            StorageEngineFactory = () => new EfCoreStorageEngine(_dbContext)
        };
        var writer = new DbAppSettingsWriter(new EfCoreStorageEngine(_dbContext), dbOpts, NullLogger<DbAppSettingsWriter>.Instance);

        await writer.SetAsync<bool>("Settings__IsEnabled", true);

        var provider = new DbAppSettingsProvider(new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            AutoMigrate = false,
            SeedOnEmpty = false,
            StorageEngineFactory = () => new EfCoreStorageEngine(_dbContext)
        });
        provider.Load();

        var config = new ConfigurationBuilder().Add(new TestProviderSource(provider)).Build();
        var reader = new DbAppSettingsReader(config);

        var readValue = reader.GetValue<bool>("Settings__IsEnabled");
        Assert.True(readValue);
    }

    private class TestProviderSource : IConfigurationSource
    {
        private readonly IConfigurationProvider _provider;
        public TestProviderSource(IConfigurationProvider provider) => _provider = provider;
        public IConfigurationProvider Build(IConfigurationBuilder builder) => _provider;
    }
}
