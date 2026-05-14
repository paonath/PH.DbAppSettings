using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PH.DbAppSettings.Data;

/// <summary>
/// Design-time factory per supportare 'dotnet ef migrations add'.
/// Usa SQLite con un file locale temporaneo.
/// </summary>
public sealed class AppSettingsDbContextFactory : IDesignTimeDbContextFactory<AppSettingsDbContext>
{
    public AppSettingsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite("Data Source=designtime.db")
            .Options;

        return new AppSettingsDbContext(options);
    }
}
