using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PH.DbAppSettings.Cli;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class DbAppSettingsCliRunnerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;
    private readonly string _connString;
    private readonly SqliteConnection _connection;

    public DbAppSettingsCliRunnerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DbAppSettingsCliRunnerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "test.db");
        _connString = $"Data Source={_dbPath}";
        _connection = new SqliteConnection(_connString);
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var storageEngine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
        services.AddSingleton<IDbAppSettingsStorageEngine>(storageEngine);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RunAsync_Analyze_OutputsFormattedSummary_AndReturnsZero()
    {
        // Arrange
        var jsonFile = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonFile, """
        {
            "ConnectionStrings": {
                "Default": "Data Source=app.db"
            },
            "Application": {
                "Title": "CLI Test App"
            }
        }
        """);

        var sp = BuildServiceProvider();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        // Act
        var exitCode = await DbAppSettingsCliRunner.RunAsync(
            sp,
            ["dbappsettings", "analyze", jsonFile],
            stdout,
            stderr);

        // Assert
        Assert.Equal(0, exitCode);
        var output = stdout.ToString();
        Assert.Contains("ConnectionStrings:Default", output);
        Assert.Contains("Application:Title", output);
        Assert.Contains("Total keys:", output);
    }

    [Fact]
    public async Task RunAsync_Import_PersistsSettingsToDatabase_AndReturnsZero()
    {
        // Arrange
        var jsonFile = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonFile, """
        {
            "App": {
                "Setting1": "Val1",
                "Setting2": "Val2"
            }
        }
        """);

        var sp = BuildServiceProvider();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        // Act
        var exitCode = await DbAppSettingsCliRunner.RunAsync(
            sp,
            ["dbappsettings", "import", jsonFile, "-e", "Production"],
            stdout,
            stderr);

        // Assert
        Assert.Equal(0, exitCode);
        var storage = sp.GetRequiredService<IDbAppSettingsStorageEngine>();
        var entries = await storage.GetAllAsync("Production");
        Assert.Contains(entries, e => e.Key == "App__Setting1" && e.Value == "Val1");
        Assert.Contains(entries, e => e.Key == "App__Setting2" && e.Value == "Val2");
    }

    [Fact]
    public async Task RunAsync_Ingest_PersistsSettings_AndDeletesSourceFile_WithYesFlag()
    {
        // Arrange
        var jsonFile = Path.Combine(_tempDir, "appsettings.ingest.json");
        await File.WriteAllTextAsync(jsonFile, """
        {
            "IngestKey": "IngestVal"
        }
        """);

        var sp = BuildServiceProvider();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        // Act
        var exitCode = await DbAppSettingsCliRunner.RunAsync(
            sp,
            ["dbappsettings", "ingest", jsonFile, "-e", "Production", "-y"],
            stdout,
            stderr);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(File.Exists(jsonFile), "Source file should be deleted on ingest -y");
        var storage = sp.GetRequiredService<IDbAppSettingsStorageEngine>();
        var entry = await storage.GetByKeyAsync("IngestKey", "Production");
        Assert.NotNull(entry);
        Assert.Equal("IngestVal", entry.Value);
    }

    [Fact]
    public async Task RunAsync_Export_WritesDatabaseRecordsToJsonFile()
    {
        // Arrange
        var sp = BuildServiceProvider();
        var storage = sp.GetRequiredService<IDbAppSettingsStorageEngine>();
        await storage.EnsureSchemaCreatedAsync();
        await storage.UpsertBatchAsync(new List<AppSettingRecord>
        {
            new() { Key = "Export__TestKey", Value = "ExportVal", Environment = "Production" }
        });

        var outputFile = Path.Combine(_tempDir, "exported.json");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        // Act
        var exitCode = await DbAppSettingsCliRunner.RunAsync(
            sp,
            ["dbappsettings", "export", outputFile, "-e", "Production"],
            stdout,
            stderr);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.Contains("Export__TestKey", content);
        Assert.Contains("ExportVal", content);
    }

    [Fact]
    public async Task RunAsync_RewriteJson_ReconstructsTypedHierarchy()
    {
        // Arrange
        var sp = BuildServiceProvider();
        var storage = sp.GetRequiredService<IDbAppSettingsStorageEngine>();
        await storage.EnsureSchemaCreatedAsync();
        await storage.UpsertBatchAsync(new List<AppSettingRecord>
        {
            new() { Key = "Application__Title", Value = "Rewritten Title", Environment = "Production" },
            new() { Key = "Application__MaxLimit", Value = "100", Environment = "Production" },
            new() { Key = "Application__IsEnabled", Value = "true", Environment = "Production" }
        });

        var outputFile = Path.Combine(_tempDir, "rewritten.json");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        // Act
        var exitCode = await DbAppSettingsCliRunner.RunAsync(
            sp,
            ["dbappsettings", "rewrite-json", outputFile, "-e", "Production"],
            stdout,
            stderr);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile));
        var content = await File.ReadAllTextAsync(outputFile);
        using var doc = JsonDocument.Parse(content);
        var app = doc.RootElement.GetProperty("Application");
        Assert.Equal("Rewritten Title", app.GetProperty("Title").GetString());
        Assert.Equal(100, app.GetProperty("MaxLimit").GetInt32());
        Assert.True(app.GetProperty("IsEnabled").GetBoolean());
    }
}
