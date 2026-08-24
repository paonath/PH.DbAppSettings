using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests.IntegrationTests;

public class BootstrapIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public BootstrapIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    private TestAppSettingsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
        var ctx = new TestAppSettingsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public async Task Test_FirstBoot_SeedsFromAppSettings()
    {
        await using var ctx = CreateContext();
        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test", SeedOnEmpty = true };
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["MyApp:Setting"] = "hello",
            ["MyApp:Nested:Value"] = "world"
        });

        var seedService = new SeedService(ctx, options, NullLogger<SeedService>.Instance);
        await seedService.SeedAsync(config);

        Assert.True(ctx.AppSettings.Any(e => e.Key == "MyApp__Setting" && e.Value == "hello"));
        Assert.True(ctx.AppSettings.Any(e => e.Key == "MyApp__Nested__Value" && e.Value == "world"));
    }

    [Fact]
    public async Task Test_SecondBoot_DoesNotOverwrite()
    {
        await using var ctx = CreateContext();
        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };

        var config1 = BuildConfig(new Dictionary<string, string?> { ["MyApp:Key"] = "original" });
        var service = new SeedService(ctx, options, NullLogger<SeedService>.Instance);
        await service.SeedAsync(config1);

        // Simulate manual DB update
        var entry = ctx.AppSettings.First(e => e.Key == "MyApp__Key");
        entry.Value = "manually-updated";
        await ctx.SaveChangesAsync();

        // Second boot with different value in config
        var config2 = BuildConfig(new Dictionary<string, string?> { ["MyApp:Key"] = "from-config" });
        await service.SeedAsync(config2);

        var value = ctx.AppSettings.First(e => e.Key == "MyApp__Key").Value;
        Assert.Equal("manually-updated", value);
    }

    [Fact]
    public async Task Test_ForceReseed_Overwrites()
    {
        await using var ctx = CreateContext();
        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };
        var service = new SeedService(ctx, options, NullLogger<SeedService>.Instance);

        var config1 = BuildConfig(new Dictionary<string, string?> { ["MyApp:Key"] = "original" });
        await service.SeedAsync(config1);

        var forceOptions = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test", ForceReseed = true };
        var forceService = new SeedService(ctx, forceOptions, NullLogger<SeedService>.Instance);
        var config2 = BuildConfig(new Dictionary<string, string?> { ["MyApp:Key"] = "overwritten" });
        await forceService.SeedAsync(config2);

        var value = ctx.AppSettings.First(e => e.Key == "MyApp__Key").Value;
        Assert.Equal("overwritten", value);
    }

    [Fact]
    public async Task Test_ExcludeKeys_NotSeeded()
    {
        await using var ctx = CreateContext();
        var options = new DbAppSettingsOptions
        {
            ConnectionString = "x",
            Environment = "Test",
            ExcludeKeysFromSeed = ["DbAppSettings:ConnectionString"]
        };
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["DbAppSettings:ConnectionString"] = "secret",
            ["MyApp:Key"] = "value"
        });

        var service = new SeedService(ctx, options, NullLogger<SeedService>.Instance);
        await service.SeedAsync(config);

        Assert.False(ctx.AppSettings.Any(e => e.Key == "DbAppSettings__ConnectionString"));
        Assert.True(ctx.AppSettings.Any(e => e.Key == "MyApp__Key"));
    }

    [Fact]
    public async Task Test_IConfiguration_ReadsFromDb()
    {
        await using var ctx = CreateContext();
        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };
        var config = BuildConfig(new Dictionary<string, string?> { ["MyApp:Setting"] = "db-value" });

        var service = new SeedService(ctx, options, NullLogger<SeedService>.Instance);
        await service.SeedAsync(config);

        // Simulate what the provider does: load from DB into a dictionary
        var data = ctx.AppSettings
            .Where(e => e.Environment == "Test")
            .ToDictionary(e => e.Key, e => e.Value);

        Assert.True(data.ContainsKey("MyApp__Setting"));
        Assert.Equal("db-value", data["MyApp__Setting"]);
    }

    [Fact]
    public async Task Test_KeyNormalization_HierarchicalAndArray()
    {
        await using var ctx = CreateContext();
        var options = new DbAppSettingsOptions { ConnectionString = "x", Environment = "Test" };
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "conn-str",
            ["Logging:LogLevel:Default"] = "Information",
            ["AllowedHosts:0"] = "localhost"
        });

        var service = new SeedService(ctx, options, NullLogger<SeedService>.Instance);
        await service.SeedAsync(config);

        var keys = ctx.AppSettings.Select(e => e.Key).ToList();
        Assert.Contains("ConnectionStrings__Default", keys);
        Assert.Contains("Logging__LogLevel__Default", keys);
        Assert.Contains("AllowedHosts__0", keys);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
