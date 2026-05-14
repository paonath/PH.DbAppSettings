using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;

namespace PH.DbAppSettings.Services;

public sealed class ReloadBackgroundService : BackgroundService
{
    private readonly DbAppSettingsProvider _provider;
    private readonly DbAppSettingsOptions _options;
    private readonly ILogger<ReloadBackgroundService> _logger;
    private Dictionary<string, string?> _lastSnapshot = new(StringComparer.OrdinalIgnoreCase);

    public ReloadBackgroundService(
        DbAppSettingsProvider provider,
        DbAppSettingsOptions options,
        ILogger<ReloadBackgroundService> logger)
    {
        _provider = provider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ReloadInterval.HasValue)
        {
            _logger.LogDebug("ReloadBackgroundService: ReloadInterval is null, service will not run.");
            return;
        }

        _logger.LogInformation("ReloadBackgroundService started. Interval: {Interval}", _options.ReloadInterval.Value);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.ReloadInterval.Value, stoppingToken);

            try
            {
                var hasChanges = await DetectChangesAsync(stoppingToken);

                if (hasChanges)
                {
                    _logger.LogInformation("Configuration changes detected. Reloading...");
                    await _provider.LoadAsync(stoppingToken);
                    _provider.TriggerReload();
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
        var dbContextOptions = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite(_options.ConnectionString)
            .Options;

        await using var dbContext = new AppSettingsDbContext(dbContextOptions);

        var currentEntries = await dbContext.AppSettings
            .Where(e => e.Environment == _options.Environment)
            .Select(e => new { e.Key, e.Value })
            .ToListAsync(ct);

        var currentSnapshot = currentEntries.ToDictionary(
            e => e.Key,
            e => e.Value,
            StringComparer.OrdinalIgnoreCase);

        if (currentSnapshot.Count != _lastSnapshot.Count)
        {
            _lastSnapshot = currentSnapshot;
            return true;
        }

        foreach (var (key, value) in currentSnapshot)
        {
            if (!_lastSnapshot.TryGetValue(key, out var lastValue) || lastValue != value)
            {
                _lastSnapshot = currentSnapshot;
                return true;
            }
        }

        return false;
    }
}
