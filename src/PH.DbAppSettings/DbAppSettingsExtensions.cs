using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Encryption;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Storage;

namespace PH.DbAppSettings;

public static class DbAppSettingsExtensions
{
    /// <summary>
    /// Aggiunge DbAppSettings come IConfigurationProvider con configurazione esplicita.
    /// </summary>
    public static IConfigurationBuilder AddDbAppSettings(
        this IConfigurationBuilder builder,
        Action<DbAppSettingsMutableOptions> configure)
    {
        var mutable = new DbAppSettingsMutableOptions();
        configure(mutable);
        var options = mutable.ToOptions();
        var currentConfig = builder.Build();
        builder.Add(new DbAppSettingsConfigurationSource(options, currentConfig));
        return builder;
    }

    /// <summary>
    /// Aggiunge DbAppSettings leggendo la ConnectionString da bootstrapConfig.
    /// </summary>
    public static IConfigurationBuilder AddDbAppSettings(
        this IConfigurationBuilder builder,
        IConfiguration bootstrapConfig,
        Action<DbAppSettingsMutableOptions>? configure = null)
    {
        var connectionString = bootstrapConfig["DbAppSettings:ConnectionString"]
            ?? bootstrapConfig["DbAppSettings__ConnectionString"]
            ?? bootstrapConfig["ConnectionStrings:DefaultConnection"]
            ?? bootstrapConfig["ConnectionStrings__DefaultConnection"]
            ?? throw new InvalidOperationException(
                "DbAppSettings connection string not found. " +
                "Set 'DbAppSettings:ConnectionString' or 'ConnectionStrings:DefaultConnection' in configuration or environment variables.");

        var environment = bootstrapConfig["ASPNETCORE_ENVIRONMENT"]
            ?? bootstrapConfig["DOTNET_ENVIRONMENT"]
            ?? "Production";

        var mutable = new DbAppSettingsMutableOptions
        {
            ConnectionString = connectionString,
            Environment = environment
        };
        configure?.Invoke(mutable);
        var options = mutable.ToOptions();

        builder.Add(new DbAppSettingsConfigurationSource(options, bootstrapConfig));
        return builder;
    }

    /// <summary>
    /// Aggiunge DbAppSettings come IConfigurationProvider per un AppSettingsDbContext derivato.
    /// </summary>
    public static IConfigurationBuilder AddDbAppSettings<TContext>(
        this IConfigurationBuilder builder,
        Action<DbAppSettingsMutableOptions> configure)
        where TContext : AppSettingsDbContext
    {
        return builder.AddDbAppSettings(configure);
    }

    /// <summary>
    /// Aggiunge DbAppSettings con bootstrapConfig per un AppSettingsDbContext derivato.
    /// </summary>
    public static IConfigurationBuilder AddDbAppSettings<TContext>(
        this IConfigurationBuilder builder,
        IConfiguration bootstrapConfig,
        Action<DbAppSettingsMutableOptions>? configure = null)
        where TContext : AppSettingsDbContext
    {
        return builder.AddDbAppSettings(bootstrapConfig, configure);
    }

    /// <summary>
    /// Registra i servizi DbAppSettings nel DI container con configurazione fluida.
    /// </summary>
    public static IServiceCollection AddDbAppSettingsServices(
        this IServiceCollection services,
        Action<DbAppSettingsMutableOptions> configure)
    {
        var mutable = new DbAppSettingsMutableOptions();
        configure(mutable);
        var options = mutable.ToOptions();
        return services.AddDbAppSettingsServices(options);
    }

    /// <summary>
    /// Registra i servizi DbAppSettings nel DI container per un AppSettingsDbContext derivato.
    /// </summary>
    public static IServiceCollection AddDbAppSettingsServices<TContext>(
        this IServiceCollection services,
        Action<DbAppSettingsMutableOptions> configure)
        where TContext : AppSettingsDbContext
    {
        var mutable = new DbAppSettingsMutableOptions();
        configure(mutable);
        var options = mutable.ToOptions();
        return services.AddDbAppSettingsServices<TContext>(options);
    }

    /// <summary>
    /// Registra i servizi DbAppSettings nel DI container per un AppSettingsDbContext derivato con DbAppSettingsOptions.
    /// </summary>
    public static IServiceCollection AddDbAppSettingsServices<TContext>(
        this IServiceCollection services,
        DbAppSettingsOptions options)
        where TContext : AppSettingsDbContext
    {
        services.AddSingleton(options);

        services.AddSingleton<IDbAppSettingsStorageEngine>(sp =>
        {
            if (options.StorageEngineFactory is not null)
            {
                return options.StorageEngineFactory();
            }

            return new EfCoreStorageEngine(
                () => sp.CreateScope().ServiceProvider.GetRequiredService<TContext>(),
                options.UseMigrations);
        });

        RegisterCommonServices(services, options);
        return services;
    }

    /// <summary>
    /// Registra i servizi DbAppSettings nel DI container.
    /// </summary>
    public static IServiceCollection AddDbAppSettingsServices(
        this IServiceCollection services,
        DbAppSettingsOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IDbAppSettingsStorageEngine>(sp =>
        {
            if (options.StorageEngineFactory is not null)
            {
                return options.StorageEngineFactory();
            }

            throw new InvalidOperationException(
                "A storage engine must be configured via options.StorageEngineFactory or by using AddDbAppSettingsServices<TContext>().");
        });

        RegisterCommonServices(services, options);
        return services;
    }

    private static void RegisterCommonServices(IServiceCollection services, DbAppSettingsOptions options)
    {
        services.AddScoped<IDbAppSettingsReader, DbAppSettingsReader>();
        services.AddScoped<IDbAppSettingsWriter, DbAppSettingsWriter>();
        services.AddTransient<SeedService>();

        if (options.EncryptValues)
        {
            var secret = System.Environment.GetEnvironmentVariable("DbAppSettings__EncryptionSecret")
                ?? throw new InvalidOperationException(
                    "Encryption is enabled but 'DbAppSettings__EncryptionSecret' environment variable is not set.");
            services.AddSingleton<IValueEncryptor>(_ => new AesGcmValueEncryptor(secret));
        }

        if (options.ReloadInterval.HasValue)
        {
            services.AddSingleton<ReloadBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<ReloadBackgroundService>());
        }
    }

    /// <summary>
    /// Intercepts execution if command-line arguments match DbAppSettings CLI commands.
    /// Returns true if a CLI command was handled, prompting the host process to terminate.
    /// </summary>
    public static bool RunDbAppSettingsCli(this Microsoft.Extensions.Hosting.IHost host, string[] args)
    {
        return RunDbAppSettingsCli(host.Services, args);
    }

    /// <summary>
    /// Intercepts execution if command-line arguments match DbAppSettings CLI commands using an IServiceProvider.
    /// </summary>
    public static bool RunDbAppSettingsCli(this IServiceProvider serviceProvider, string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        var first = args[0];
        if (!first.Equals(Cli.DbAppSettingsCliRunner.CliPrefix, StringComparison.OrdinalIgnoreCase) &&
            !first.Equals($"--{Cli.DbAppSettingsCliRunner.CliPrefix}", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var exitCode = Cli.DbAppSettingsCliRunner.RunAsync(serviceProvider, args).GetAwaiter().GetResult();
        return true;
    }
}
