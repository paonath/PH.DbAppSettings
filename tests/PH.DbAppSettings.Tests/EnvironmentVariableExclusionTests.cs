using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class EnvironmentVariableExclusionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;
    private readonly string _tempDir;
    private const string TestEnvVarKey = "MY_SPECIAL_TEST_SECRET_ENV_VAR";

    public EnvironmentVariableExclusionTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();

        _tempDir = Path.Combine(Path.GetTempPath(), "PH.DbAppSettings.EnvTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        Environment.SetEnvironmentVariable(TestEnvVarKey, "secret-token-12345");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(TestEnvVarKey, null);
        Environment.SetEnvironmentVariable("Application__Title", null);
        _connection.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private DapperStorageEngine CreateStorageEngine()
    {
        return new DapperStorageEngine(
            () => new SqliteConnection(_connString),
            new SqliteDialect(),
            "",
            "AppSettings");
    }

    [Fact]
    public async Task SeedService_WhenBootstrapHasEnvironmentVariables_SeedsOnlyJsonKeys()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonPath, """
            {
              "Application": {
                "Title": "My Pure JSON App",
                "MaxPageSize": 50
              }
            }
            """);

        var bootstrapConfig = new ConfigurationBuilder()
            .AddJsonFile(jsonPath)
            .AddEnvironmentVariables()
            .Build();

        // Verify that bootstrapConfig contains the environment variable in memory
        Assert.Equal("secret-token-12345", bootstrapConfig[TestEnvVarKey]);

        var storageEngine = CreateStorageEngine();
        await storageEngine.EnsureSchemaCreatedAsync();

        var options = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Production",
            AutoMigrate = false,
            SeedOnEmpty = true
        };

        var seedService = new SeedService(storageEngine, options, NullLogger<SeedService>.Instance);

        // Act
        await seedService.SeedAsync(bootstrapConfig);

        // Assert
        var entries = await storageEngine.GetAllAsync("Production");

        // Database MUST only contain the 2 keys from appsettings.json
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Key == "Application__Title" && e.Value == "My Pure JSON App");
        Assert.Contains(entries, e => e.Key == "Application__MaxPageSize" && e.Value == "50");

        // Database MUST NOT contain environment variable
        Assert.DoesNotContain(entries, e => e.Key.Contains(TestEnvVarKey, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e => e.Key.Equals("PATH", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, e => e.Key.Equals("USER", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeedService_WhenEnvVarOverridesJsonKey_SeedsJsonValueToDb_AndKeepsEnvInConfig()
    {
        // Arrange
        Environment.SetEnvironmentVariable("Application__Title", "Env Override Title");

        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonPath, """
            {
              "Application": {
                "Title": "Original JSON Title"
              }
            }
            """);

        var bootstrapConfig = new ConfigurationBuilder()
            .AddJsonFile(jsonPath)
            .AddEnvironmentVariables()
            .Build();

        // In memory, Configuration honors the Environment Variable override
        Assert.Equal("Env Override Title", bootstrapConfig["Application:Title"]);

        var storageEngine = CreateStorageEngine();
        await storageEngine.EnsureSchemaCreatedAsync();

        var options = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Production",
            AutoMigrate = false,
            SeedOnEmpty = true
        };

        var seedService = new SeedService(storageEngine, options, NullLogger<SeedService>.Instance);

        // Act
        await seedService.SeedAsync(bootstrapConfig);

        // Assert
        var entries = await storageEngine.GetAllAsync("Production");

        // The database must be seeded with the value from the JSON file
        Assert.Single(entries);
        var entry = entries.First();
        Assert.Equal("Application__Title", entry.Key);
        Assert.Equal("Original JSON Title", entry.Value);
    }
}
