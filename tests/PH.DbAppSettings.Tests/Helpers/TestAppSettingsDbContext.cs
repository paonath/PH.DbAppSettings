using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;

namespace PH.DbAppSettings.Tests.Helpers;

public class TestAppSettingsDbContext : AppSettingsDbContext<TestAppSettingsDbContext>
{
    public TestAppSettingsDbContext(DbContextOptions<TestAppSettingsDbContext> options) : base(options)
    {
    }

    public TestAppSettingsDbContext() : base()
    {
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
