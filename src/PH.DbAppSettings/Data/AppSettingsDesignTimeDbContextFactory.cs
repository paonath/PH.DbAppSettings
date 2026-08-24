using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PH.DbAppSettings.Data;

/// <summary>
/// Abstract base class for EF Core design-time DbContext factory.
/// Inherit this class in your application to enable 'dotnet ef migrations' support for your AppSettingsDbContext-derived context.
/// </summary>
/// <typeparam name="TContext">The concrete DbContext type derived from AppSettingsDbContext.</typeparam>
public abstract class AppSettingsDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : AppSettingsDbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        ConfigureOptionsBuilder(optionsBuilder, connectionString);

        return CreateDbContextInstance(optionsBuilder.Options);
    }

    /// <summary>
    /// Configures the DbContextOptionsBuilder with provider and connection string.
    /// </summary>
    protected abstract void ConfigureOptionsBuilder(
        DbContextOptionsBuilder<TContext> builder,
        string connectionString);

    /// <summary>
    /// Resolves the connection string from args, environment variables, or defaults.
    /// </summary>
    protected virtual string ResolveConnectionString(string[] args)
    {
        return Environment.GetEnvironmentVariable("DbAppSettings__ConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Data Source=appsettings.db";
    }

    /// <summary>
    /// Instantiates the concrete DbContext using the configured options.
    /// </summary>
    protected virtual TContext CreateDbContextInstance(DbContextOptions<TContext> options)
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}
