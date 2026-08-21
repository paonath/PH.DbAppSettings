using System.Text.Json;
using Microsoft.Data.Sqlite;
using PH.DbAppSettings.Cli.Commands;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class CliCommandTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;
    private readonly string _tempDir;

    public CliCommandTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();

        _tempDir = Path.Combine(Path.GetTempPath(), "PH.DbAppSettings.CliTests", Guid.NewGuid().ToString("N"));
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
    public async Task AnalyzeCommand_ExecutesAndOutputsAnalysis()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonPath, """
            {
              "Logging": { "LogLevel": { "Default": "Information" } },
              "SecretKey": "super-secret"
            }
            """);

        var command = new AnalyzeCommand();

        // Act
        var exitCode = await command.ExecuteAsync(new AnalyzeCommandSettings
        {
            FilePath = jsonPath
        });

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ImportCommand_ImportsJsonFile_IntoStorageEngine()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        await File.WriteAllTextAsync(jsonPath, """
            {
              "App": {
                "Title": "My Demo App",
                "MaxItems": 42
              }
            }
            """);

        var engine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
        await engine.EnsureSchemaCreatedAsync();

        var command = new ImportCommand();

        // Act
        var exitCode = await command.ExecuteAsync(new ImportCommandSettings
        {
            FilePath = jsonPath,
            ConnectionString = _connString,
            Dialect = "sqlite",
            Environment = "Production",
            AutoMigrate = true
        });

        // Assert
        Assert.Equal(0, exitCode);
        var entries = await engine.GetAllAsync("Production");
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Key == "App__Title" && e.Value == "My Demo App");
        Assert.Contains(entries, e => e.Key == "App__MaxItems" && e.Value == "42");
    }

    [Fact]
    public async Task ExportCommand_ExportsDatabase_ToJsonFile()
    {
        // Arrange
        var engine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
        await engine.EnsureSchemaCreatedAsync();

        await engine.UpsertBatchAsync(new List<AppSettingRecord>
        {
            new() { Key = "App__Title", Environment = "Production", Value = "Exported Title" },
            new() { Key = "App__MaxItems", Environment = "Production", Value = "100" }
        });

        var exportPath = Path.Combine(_tempDir, "exported.json");
        var command = new ExportCommand();

        // Act
        var exitCode = await command.ExecuteAsync(new ExportCommandSettings
        {
            OutputPath = exportPath,
            ConnectionString = _connString,
            Dialect = "sqlite",
            Environment = "Production"
        });

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(exportPath));

        var exportedJson = await File.ReadAllTextAsync(exportPath);
        using var doc = JsonDocument.Parse(exportedJson);
        var appObj = doc.RootElement.GetProperty("App");
        Assert.Equal("Exported Title", appObj.GetProperty("Title").GetString());
        Assert.Equal("100", appObj.GetProperty("MaxItems").GetString());
    }
}
