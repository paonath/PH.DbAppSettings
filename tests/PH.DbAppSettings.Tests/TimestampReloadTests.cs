using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class TimestampReloadTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _connString;

    public TimestampReloadTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        _connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(_connString);
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
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
    public async Task ReloadBackgroundService_PollsTimestamp_And_ReloadsWhenChanged()
    {
        // Arrange
        var storageEngine = CreateStorageEngine();
        await storageEngine.EnsureSchemaCreatedAsync();

        var initialTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        await storageEngine.UpsertAsync(new AppSettingRecord
        {
            Key = "Settings__Timeout",
            Environment = "Test",
            Value = "30",
            UpdatedAt = initialTime
        });

        var options = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test",
            ReloadInterval = TimeSpan.FromMilliseconds(50),
            AutoMigrate = false,
            SeedOnEmpty = false
        };

        var provider = new DbAppSettingsProvider(options, storageEngine: storageEngine);
        await provider.LoadAsync();

        Assert.True(provider.TryGet("Settings:Timeout", out var initialVal));
        Assert.Equal("30", initialVal);

        var reloadService = new ReloadBackgroundService(
            provider,
            options,
            storageEngine,
            NullLogger<ReloadBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await reloadService.StartAsync(cts.Token);
        await Task.Delay(100);

        // Act: Update storage engine with a newer timestamp
        var updatedTime = DateTimeOffset.UtcNow;
        await storageEngine.UpsertAsync(new AppSettingRecord
        {
            Key = "Settings__Timeout",
            Environment = "Test",
            Value = "60",
            UpdatedAt = updatedTime
        });

        // Wait for background reload cycle with polling
        var reloaded = false;
        for (var i = 0; i < 30; i++)
        {
            if (provider.TryGet("Settings:Timeout", out var currentVal) && currentVal == "60")
            {
                reloaded = true;
                break;
            }
            await Task.Delay(50);
        }

        // Assert: Provider should have updated value
        Assert.True(reloaded, "Expected provider to reload updated value '60' within timeout window.");
        Assert.True(provider.TryGet("Settings:Timeout", out var updatedVal));
        Assert.Equal("60", updatedVal);

        // Cleanup
        await cts.CancelAsync();
        await reloadService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DbAppSettingsWriter_UsesStorageEngine_And_UpdatesTimestamp()
    {
        // Arrange
        var storageEngine = CreateStorageEngine();
        await storageEngine.EnsureSchemaCreatedAsync();

        var options = new DbAppSettingsOptions
        {
            ConnectionString = _connString,
            Environment = "Test"
        };

        var writer = new DbAppSettingsWriter(
            storageEngine,
            options,
            NullLogger<DbAppSettingsWriter>.Instance);

        // Act & Assert 1: Insert
        await writer.SetAsync("MyApp:Feature:Cache", "true");
        var record = await storageEngine.GetByKeyAsync("MyApp__Feature__Cache", "Test");
        Assert.NotNull(record);
        Assert.Equal("true", record.Value);
        Assert.NotNull(record.UpdatedAt);

        // Act & Assert 2: Delete
        await writer.DeleteAsync("MyApp:Feature:Cache");
        var deletedRecord = await storageEngine.GetByKeyAsync("MyApp__Feature__Cache", "Test");
        Assert.Null(deletedRecord);
    }
}
