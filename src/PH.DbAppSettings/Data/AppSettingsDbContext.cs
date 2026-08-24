using Microsoft.EntityFrameworkCore;

namespace PH.DbAppSettings.Data;

/// <summary>
/// Abstract base DbContext providing configuration storage entities and mapping.
/// Inherit this class in your application's DbContext to co-locate AppSettings in the application database.
/// </summary>
public abstract class AppSettingsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with non-generic DbContextOptions.
    /// </summary>
    protected AppSettingsDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Parameterless constructor for design-time tooling or derived custom factories.
    /// </summary>
    protected AppSettingsDbContext() : base()
    {
    }

    /// <summary>
    /// DbSet for application settings entries stored in the database.
    /// </summary>
    public DbSet<AppSettingEntry> AppSettings => Set<AppSettingEntry>();

    /// <summary>
    /// Applies the AppSettingEntry entity configuration.
    /// Derived classes must call base.OnModelCreating(modelBuilder).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AppSettingEntryConfiguration());
    }
}

/// <summary>
/// Generic abstract base DbContext for strongly typed DbContextOptions.
/// </summary>
/// <typeparam name="TContext">The derived DbContext type.</typeparam>
public abstract class AppSettingsDbContext<TContext> : AppSettingsDbContext where TContext : DbContext
{
    /// <summary>
    /// Initializes a new instance with strongly typed DbContextOptions.
    /// </summary>
    protected AppSettingsDbContext(DbContextOptions<TContext> options) : base(options)
    {
    }

    /// <summary>
    /// Parameterless constructor for design-time tooling or derived custom factories.
    /// </summary>
    protected AppSettingsDbContext() : base()
    {
    }
}
