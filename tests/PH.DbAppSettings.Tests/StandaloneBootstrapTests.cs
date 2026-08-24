using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public sealed record StandaloneAppOptions
{
    public string Title { get; init; } = "";
    public int MaxItems { get; init; }
    public bool IsActive { get; init; }
}

public class StandaloneBootstrapTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;

    public StandaloneBootstrapTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        Environment.SetEnvironmentVariable("DbAppSettings__ConnectionString", null);
        Environment.SetEnvironmentVariable("DbAppSettings:ConnectionString", null);
    }

    [Fact]
    public async Task Application_StartsAndBindsOptions_WithoutAnyJsonFileOnDisk()
    {
        // Arrange: Pre-populate database
        var engine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
        await engine.EnsureSchemaCreatedAsync();

        await engine.UpsertBatchAsync(new List<AppSettingRecord>
        {
            new() { Key = "Application__Title", Environment = "Production", Value = "Standalone App" },
            new() { Key = "Application__MaxItems", Environment = "Production", Value = "42" },
            new() { Key = "Application__IsActive", Environment = "Production", Value = "true" }
        });

        // Set ConnectionString via Environment Variable ONLY (no appsettings.json on disk)
        Environment.SetEnvironmentVariable("DbAppSettings__ConnectionString", _connString);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        // Act: Bootstrap configuration using only Environment Variables + DbAppSettings
        var bootstrapConfig = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var configuration = new ConfigurationBuilder()
            .AddDbAppSettings(bootstrapConfig, options =>
            {
                options.UseDapperSqlite(_connString);
                options.AutoMigrate = false;
                options.SeedOnEmpty = false;
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<StandaloneAppOptions>(configuration.GetSection("Application"));

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<StandaloneAppOptions>>().Value;

        // Assert: Options are fully populated from SQL table
        Assert.NotNull(options);
        Assert.Equal("Standalone App", options.Title);
        Assert.Equal(42, options.MaxItems);
        Assert.True(options.IsActive);
    }
}
