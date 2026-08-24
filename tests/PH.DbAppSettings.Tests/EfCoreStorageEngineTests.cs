using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class EfCoreStorageEngineTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestAppSettingsDbContext> _dbContextOptions;

    public EfCoreStorageEngineTests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var connString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(connString);
        _connection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private EfCoreStorageEngine CreateEngine(bool useMigrations = false)
    {
        return new EfCoreStorageEngine(() => new TestAppSettingsDbContext(_dbContextOptions), useMigrations);
    }

    [Fact]
    public async Task EnsureSchemaCreatedAsync_CreatesTableOnEmptyDatabase()
    {
        // Arrange
        var engine = CreateEngine(useMigrations: false);

        // Act
        await engine.EnsureSchemaCreatedAsync();
        var isEmpty = await engine.IsEmptyAsync("Test");

        // Assert
        Assert.True(isEmpty);
    }

    [Fact]
    public void FromContext_CreatesValidEngineInstance()
    {
        // Act
        var engine = EfCoreStorageEngine.FromContext(() => new TestAppSettingsDbContext(_dbContextOptions));

        // Assert
        Assert.NotNull(engine);
    }

    [Fact]
    public async Task UpsertAsync_And_GetByKeyAsync_RoundTrip()
    {
        // Arrange
        var engine = CreateEngine();
        await engine.EnsureSchemaCreatedAsync();

        var entry = new AppSettingRecord
        {
            Key = "Smtp__Host",
            Environment = "Test",
            Value = "smtp.example.com",
            IsEncrypted = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        await engine.UpsertAsync(entry);
        var retrieved = await engine.GetByKeyAsync("Smtp__Host", "Test");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Smtp__Host", retrieved.Key);
        Assert.Equal("Test", retrieved.Environment);
        Assert.Equal("smtp.example.com", retrieved.Value);
        Assert.False(retrieved.IsEncrypted);
        Assert.NotNull(retrieved.UpdatedAt);
    }

    [Fact]
    public async Task UpsertBatchAsync_And_GetAllAsync_ReturnsAllRecords()
    {
        // Arrange
        var engine = CreateEngine();
        await engine.EnsureSchemaCreatedAsync();

        var entries = new List<AppSettingRecord>
        {
            new() { Key = "App__Key1", Environment = "Test", Value = "Val1" },
            new() { Key = "App__Key2", Environment = "Test", Value = "Val2" },
            new() { Key = "App__Key3", Environment = "OtherEnv", Value = "Val3" }
        };

        // Act
        await engine.UpsertBatchAsync(entries);
        var testEntries = await engine.GetAllAsync("Test");

        // Assert
        Assert.Equal(2, testEntries.Count);
        Assert.Contains(testEntries, e => e.Key == "App__Key1" && e.Value == "Val1");
        Assert.Contains(testEntries, e => e.Key == "App__Key2" && e.Value == "Val2");
    }

    [Fact]
    public async Task GetLastModifiedTimestampAsync_ReturnsLatestTimestamp()
    {
        // Arrange
        var engine = CreateEngine();
        await engine.EnsureSchemaCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        await engine.UpsertAsync(new AppSettingRecord
        {
            Key = "Key1",
            Environment = "Test",
            Value = "Val1",
            UpdatedAt = now
        });

        // Act
        var latestTimestamp = await engine.GetLastModifiedTimestampAsync("Test");

        // Assert
        Assert.NotNull(latestTimestamp);
        Assert.True(Math.Abs((latestTimestamp.Value - now).TotalSeconds) < 2);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord()
    {
        // Arrange
        var engine = CreateEngine();
        await engine.EnsureSchemaCreatedAsync();

        await engine.UpsertAsync(new AppSettingRecord
        {
            Key = "KeyToDelete",
            Environment = "Test",
            Value = "Val"
        });

        // Act
        var deleted = await engine.DeleteAsync("KeyToDelete", "Test");
        var retrieved = await engine.GetByKeyAsync("KeyToDelete", "Test");

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }
}
