using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class SeedServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly TestAppSettingsDbContext _dbContext;

    public SeedServiceTests()
    {
        (_dbContext, _connection) = DbContextHelper.CreateSharedInMemoryContext();
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public async Task SeedAsync_SeedsHierarchicalKeys()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["MyApp:Setting1"] = "value1",
            ["MyApp:Nested:Setting2"] = "value2"
        });

        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };
        var service = new SeedService(_dbContext, options, NullLogger<SeedService>.Instance);

        await service.SeedAsync(config);

        var keys = _dbContext.AppSettings.Select(e => e.Key).ToList();
        Assert.Contains("MyApp__Setting1", keys);
        Assert.Contains("MyApp__Nested__Setting2", keys);
    }

    [Fact]
    public async Task SeedAsync_ExcludesKeysInExcludeList()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["DbAppSettings:ConnectionString"] = "secret-conn",
            ["MyApp:Setting"] = "value"
        });

        var options = new DbAppSettingsOptions
        {
            ConnectionString = "x",
            Environment = "Test",
            ExcludeKeysFromSeed = ["DbAppSettings:ConnectionString"]
        };
        var service = new SeedService(_dbContext, options, NullLogger<SeedService>.Instance);

        await service.SeedAsync(config);

        var keys = _dbContext.AppSettings.Select(e => e.Key).ToList();
        Assert.DoesNotContain("DbAppSettings__ConnectionString", keys);
        Assert.Contains("MyApp__Setting", keys);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_DoesNotDuplicateRows()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["MyApp:Key"] = "original"
        });

        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };
        var service = new SeedService(_dbContext, options, NullLogger<SeedService>.Instance);

        await service.SeedAsync(config);
        await service.SeedAsync(config);

        var count = _dbContext.AppSettings.Count(e => e.Key == "MyApp__Key");
        Assert.Equal(1, count);

        var value = _dbContext.AppSettings.First(e => e.Key == "MyApp__Key").Value;
        Assert.Equal("original", value);
    }

    [Fact]
    public async Task SeedAsync_ForceReseed_OverwritesExistingValues()
    {
        var config1 = BuildConfig(new Dictionary<string, string?> { ["MyApp:Key"] = "original" });
        var config2 = BuildConfig(new Dictionary<string, string?> { ["MyApp:Key"] = "updated" });

        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };
        var service = new SeedService(_dbContext, options, NullLogger<SeedService>.Instance);
        await service.SeedAsync(config1);

        var forceOptions = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test", ForceReseed = true };
        var forceService = new SeedService(_dbContext, forceOptions, NullLogger<SeedService>.Instance);
        await forceService.SeedAsync(config2);

        var value = _dbContext.AppSettings.First(e => e.Key == "MyApp__Key").Value;
        Assert.Equal("updated", value);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
