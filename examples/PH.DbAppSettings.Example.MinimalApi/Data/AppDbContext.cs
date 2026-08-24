using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;

namespace PH.DbAppSettings.Example.MinimalApi.Data;

/// <summary>
/// Application DbContext inheriting AppSettingsDbContext to co-locate configuration data with application entities.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : AppSettingsDbContext<AppDbContext>(options)
{
}
