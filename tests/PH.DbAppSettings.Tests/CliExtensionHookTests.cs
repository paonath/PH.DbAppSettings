using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class CliExtensionHookTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _connString;
    private readonly SqliteConnection _connection;

    public CliExtensionHookTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CliExtensionHookTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _connString = $"Data Source={Path.Combine(_tempDir, "hook.db")}";
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

    private IHost BuildHost()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices((_, services) =>
        {
            var storageEngine = new DapperStorageEngine(() => new SqliteConnection(_connString), new SqliteDialect(), "", "AppSettings");
            services.AddSingleton<IDbAppSettingsStorageEngine>(storageEngine);
        });
        return builder.Build();
    }

    [Fact]
    public void RunDbAppSettingsCli_WithNonCliArgs_ReturnsFalse()
    {
        // Arrange
        using var host = BuildHost();
        string[] args = ["--urls", "http://localhost:5000", "--environment", "Development"];

        // Act
        var handled = host.RunDbAppSettingsCli(args);

        // Assert
        Assert.False(handled);
    }

    [Fact]
    public void RunDbAppSettingsCli_WithDbAppSettingsArgs_ReturnsTrue()
    {
        // Arrange
        using var host = BuildHost();
        var jsonFile = Path.Combine(_tempDir, "hook.json");
        File.WriteAllText(jsonFile, "{\"HookKey\": \"HookVal\"}");
        string[] args = ["dbappsettings", "analyze", jsonFile];

        // Act
        var handled = host.RunDbAppSettingsCli(args);

        // Assert
        Assert.True(handled);
    }
}
