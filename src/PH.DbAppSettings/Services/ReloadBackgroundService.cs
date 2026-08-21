using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Storage;

namespace PH.DbAppSettings.Services;

public sealed class ReloadBackgroundService : BackgroundService
{
    private readonly DbAppSettingsProvider? _provider;
    private readonly IConfiguration? _configuration;
    private readonly DbAppSettingsOptions _options;
    private readonly IDbAppSettingsStorageEngine _storageEngine;
    private readonly ILogger<ReloadBackgroundService> _logger;
    private DateTimeOffset _lastSeenTimestamp = DateTimeOffset.MinValue;

    [ActivatorUtilitiesConstructor]
    public ReloadBackgroundService(
        IConfiguration configuration,
        DbAppSettingsOptions options,
        IDbAppSettingsStorageEngine storageEngine,
        ILogger<ReloadBackgroundService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _storageEngine = storageEngine ?? throw new ArgumentNullException(nameof(storageEngine));
        _logger = logger;

        if (_configuration is IConfigurationRoot root)
        {
            _provider = root.Providers.OfType<DbAppSettingsProvider>().FirstOrDefault();
        }
    }

    public ReloadBackgroundService(
        DbAppSettingsProvider provider,
        DbAppSettingsOptions options,
        IDbAppSettingsStorageEngine storageEngine,
        ILogger<ReloadBackgroundService> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _storageEngine = storageEngine ?? throw new ArgumentNullException(nameof(storageEngine));
        _logger = logger;
    }

    public ReloadBackgroundService(
        DbAppSettingsProvider provider,
        DbAppSettingsOptions options,
        ILogger<ReloadBackgroundService> logger)
        : this(
            provider,
            options,
            new EfCoreStorageEngine(() => new AppSettingsDbContext(
                new DbContextOptionsBuilder<AppSettingsDbContext>().UseSqlite(options.ConnectionString).Options)),
            logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ReloadInterval.HasValue)
        {
            _logger.LogDebug("ReloadBackgroundService: ReloadInterval is null, service will not run.");
            return;
        }

        _logger.LogInformation("ReloadBackgroundService started. Interval: {Interval}", _options.ReloadInterval.Value);

        // Initialize timestamp
        var initialTimestamp = await _storageEngine.GetLastModifiedTimestampAsync(_options.Environment, stoppingToken);
        if (initialTimestamp.HasValue)
        {
            _lastSeenTimestamp = initialTimestamp.Value;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.ReloadInterval.Value, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                var hasChanges = await DetectChangesAsync(stoppingToken);

                if (hasChanges)
                {
                    _logger.LogInformation("Configuration changes detected via timestamp. Reloading...");
                    if (_provider is not null)
                    {
                        await _provider.LoadAsync(stoppingToken);
                        _provider.TriggerReload();
                    }
                    else if (_configuration is IConfigurationRoot root)
                    {
                        root.Reload();
                    }
                    _logger.LogInformation("Configuration reloaded successfully.");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration reload check.");
            }
        }
    }

    private async Task<bool> DetectChangesAsync(CancellationToken ct)
    {
        var latestTimestamp = await _storageEngine.GetLastModifiedTimestampAsync(_options.Environment, ct);
        if (!latestTimestamp.HasValue)
        {
            return false;
        }

        if (_lastSeenTimestamp == DateTimeOffset.MinValue || latestTimestamp.Value > _lastSeenTimestamp)
        {
            _lastSeenTimestamp = latestTimestamp.Value;
            return true;
        }

        return false;
    }
}
