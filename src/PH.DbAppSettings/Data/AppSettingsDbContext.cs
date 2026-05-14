using Microsoft.EntityFrameworkCore;

namespace PH.DbAppSettings.Data;

public class AppSettingsDbContext(DbContextOptions<AppSettingsDbContext> options) : DbContext(options)
{
    public DbSet<AppSettingEntry> AppSettings => Set<AppSettingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AppSettingEntryConfiguration());
    }
}
