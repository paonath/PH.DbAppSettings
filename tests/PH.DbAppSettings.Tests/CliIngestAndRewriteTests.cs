using System.Text.Json;
using Microsoft.Data.Sqlite;
using PH.DbAppSettings.Cli.Commands;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class CliIngestAndRewriteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;
    private readonly string _tempDir;

    public CliIngestAndRewriteTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();

        _tempDir = Path.Combine(Path.GetTempPath(), "PH.DbAppSettings.IngestTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task IngestCommand_ImportsIntoDatabase_AndDeletesSourceFile()
    {
        // Arrange: Create a temporary appsettings.json file
        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonPath, """
            {
              "Application": {
                "Name": "Ingest Target",
                "Port": 5000
              }
            }
            """);

        var engine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
        await engine.EnsureSchemaCreatedAsync();

        var command = new IngestCommand();

        // Act: Run ingest command with automatic confirmation flag (-y)
        var exitCode = await command.ExecuteAsync(new IngestCommandSettings
        {
            FilePath = jsonPath,
            ConnectionString = _connString,
            Dialect = "sqlite",
            Environment = "Production",
            AutoMigrate = true,
            Yes = true
        });

        // Assert
        Assert.Equal(0, exitCode);

        // 1. Source JSON file MUST be deleted
        Assert.False(File.Exists(jsonPath), "Expected source appsettings.json to be deleted after ingestion.");

        // 2. Database table MUST contain the imported keys
        var entries = await engine.GetAllAsync("Production");
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Key == "Application__Name" && e.Value == "Ingest Target");
        Assert.Contains(entries, e => e.Key == "Application__Port" && e.Value == "5000");
    }

    [Fact]
    public async Task RewriteJsonCommand_ReconstructsFormattedJson_WithTypesAndArrays()
    {
        // Arrange: Pre-populate database with string, number, boolean, and array keys
        var engine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
        await engine.EnsureSchemaCreatedAsync();

        await engine.UpsertBatchAsync(new List<AppSettingRecord>
        {
            new() { Key = "App__Name", Environment = "Production", Value = "Reconstructed App" },
            new() { Key = "App__Port", Environment = "Production", Value = "8080" },
            new() { Key = "App__IsActive", Environment = "Production", Value = "true" },
            new() { Key = "App__AllowedOrigins__0", Environment = "Production", Value = "https://alpha.com" },
            new() { Key = "App__AllowedOrigins__1", Environment = "Production", Value = "https://beta.com" }
        });

        var outJsonPath = Path.Combine(_tempDir, "appsettings.rewritten.json");
        var command = new RewriteJsonCommand();

        // Act: Reconstruct JSON configuration file from SQL database
        var exitCode = await command.ExecuteAsync(new RewriteJsonCommandSettings
        {
            OutputPath = outJsonPath,
            ConnectionString = _connString,
            Dialect = "sqlite",
            Environment = "Production"
        });

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outJsonPath), "Expected rewritten JSON file to exist.");

        var jsonContent = await File.ReadAllTextAsync(outJsonPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;
        var appObj = root.GetProperty("App");

        // Assert Types
        Assert.Equal("Reconstructed App", appObj.GetProperty("Name").GetString());
        Assert.Equal(8080, appObj.GetProperty("Port").GetInt32());
        Assert.True(appObj.GetProperty("IsActive").GetBoolean());

        // Assert Array Reconstruction
        var origins = appObj.GetProperty("AllowedOrigins");
        Assert.Equal(JsonValueKind.Array, origins.ValueKind);
        Assert.Equal(2, origins.GetArrayLength());
        Assert.Equal("https://alpha.com", origins[0].GetString());
        Assert.Equal("https://beta.com", origins[1].GetString());
    }
}
