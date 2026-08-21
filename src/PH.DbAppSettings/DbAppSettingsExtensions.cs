using Microsoft.EntityFrameworkCore;
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
    /// Deve essere chiamato DOPO aver aggiunto appsettings.json.
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
    /// Aggiunge DbAppSettings leggendo la ConnectionString da bootstrapConfig
    /// (chiave: "DbAppSettings:ConnectionString" o env var "DbAppSettings__ConnectionString").
    /// </summary>
    public static IConfigurationBuilder AddDbAppSettings(
        this IConfigurationBuilder builder,
        IConfiguration bootstrapConfig,
        Action<DbAppSettingsMutableOptions>? configure = null)
    {
        var connectionString = bootstrapConfig["DbAppSettings:ConnectionString"]
            ?? bootstrapConfig["DbAppSettings__ConnectionString"]
            ?? throw new InvalidOperationException(
                "DbAppSettings connection string not found. " +
                "Set 'DbAppSettings:ConnectionString' in appsettings.json or " +
                "'DbAppSettings__ConnectionString' as environment variable.");

        var environment = bootstrapConfig["ASPNETCORE_ENVIRONMENT"] ?? "Production";

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

            return new EfCoreStorageEngine(() =>
            {
                var dbOpts = new DbContextOptionsBuilder<AppSettingsDbContext>()
                    .UseSqlite(options.ConnectionString)
                    .Options;
                return new AppSettingsDbContext(dbOpts);
            });
        });

        services.AddDbContext<AppSettingsDbContext>(dbOpts =>
        {
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                dbOpts.UseSqlite(options.ConnectionString);
            }
        });

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

        return services;
    }
}
