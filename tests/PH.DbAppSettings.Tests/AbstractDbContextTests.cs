using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class AbstractDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public AbstractDbContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void AppSettingsDbContext_IsAbstract()
    {
        // Assert
        Assert.True(typeof(AppSettingsDbContext).IsAbstract);
        Assert.True(typeof(AppSettingsDbContext<>).IsAbstract);
    }

    [Fact]
    public void DerivedContext_InheritsFromAppSettingsDbContext()
    {
        // Assert
        Assert.True(typeof(AppSettingsDbContext).IsAssignableFrom(typeof(TestAppSettingsDbContext)));
    }

    [Fact]
    public async Task DerivedContext_CreatesSchemaAndAccessesAppSettingsDbSet()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var context = new TestAppSettingsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Act
        context.AppSettings.Add(new AppSettingEntry
        {
            Key = "Test__Key",
            Environment = "Production",
            Value = "TestValue",
            IsEncrypted = false,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var entry = await context.AppSettings
            .FirstOrDefaultAsync(e => e.Key == "Test__Key" && e.Environment == "Production");

        // Assert
        Assert.NotNull(entry);
        Assert.Equal("TestValue", entry.Value);
    }

    [Fact]
    public void ModelBuilder_ContainsAppSettingEntryEntity_WithCompositeKey()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TestAppSettingsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(AppSettingEntry));

        // Assert
        Assert.NotNull(entityType);
        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(2, primaryKey.Properties.Count);
        Assert.Contains(primaryKey.Properties, p => p.Name == nameof(AppSettingEntry.Key));
        Assert.Contains(primaryKey.Properties, p => p.Name == nameof(AppSettingEntry.Environment));
    }
}
