using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Tests.Helpers;

namespace PH.DbAppSettings.Tests;

public class DesignTimeFactoryTests
{
    public class TestFactory : AppSettingsDesignTimeDbContextFactory<TestAppSettingsDbContext>
    {
        protected override void ConfigureOptionsBuilder(
            DbContextOptionsBuilder<TestAppSettingsDbContext> builder,
            string connectionString)
        {
            builder.UseSqlite(connectionString);
        }

        protected override string ResolveConnectionString(string[] args)
        {
            return "Data Source=:memory:";
        }
    }

    [Fact]
    public void CreateDbContext_ReturnsConfiguredDbContextInstance()
    {
        // Arrange
        var factory = new TestFactory();

        // Act
        using var context = factory.CreateDbContext([]);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TestAppSettingsDbContext>(context);
        Assert.NotNull(context.AppSettings);
    }
}
