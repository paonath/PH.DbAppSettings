using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Encryption;
using PH.DbAppSettings.Services;
using PH.DbAppSettings.Storage;

namespace PH.DbAppSettings.Configuration;

public sealed class DbAppSettingsProvider : ConfigurationProvider
{
    private readonly DbAppSettingsOptions _options;
    private readonly IConfiguration? _bootstrapConfig;
    private readonly ILogger<DbAppSettingsProvider> _logger;
    private readonly IValueEncryptor? _encryptor;
    private readonly IDbAppSettingsStorageEngine? _storageEngine;

    public DbAppSettingsProvider(
        DbAppSettingsOptions options,
        IConfiguration? bootstrapConfig = null,
        ILogger<DbAppSettingsProvider>? logger = null,
        IValueEncryptor? encryptor = null,
        IDbAppSettingsStorageEngine? storageEngine = null)
    {
        _options = options;
        _bootstrapConfig = bootstrapConfig;
        _logger = logger ?? NullLogger<DbAppSettingsProvider>.Instance;
        _encryptor = encryptor;
        _storageEngine = storageEngine;
    }

    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var engine = _storageEngine ?? CreateDefaultEfEngine();

        if (_options.AutoMigrate)
        {
            _logger.LogInformation("Ensuring database schema is created...");
            await engine.EnsureSchemaCreatedAsync(ct);
        }

        if (_options.SeedOnEmpty && _bootstrapConfig is not null)
        {
            var isEmpty = await engine.IsEmptyAsync(_options.Environment, ct);
            if (isEmpty)
            {
                _logger.LogInformation("Database is empty. Starting seed from bootstrap configuration...");
                var seedLogger = NullLogger<SeedService>.Instance;
                var seedService = new SeedService(engine, _options, seedLogger, _encryptor);
                await seedService.SeedAsync(_bootstrapConfig, ct);
            }
        }

        var entries = await engine.GetAllAsync(_options.Environment, ct);
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var value = entry.Value;

            if (entry.IsEncrypted && _encryptor is not null && value is not null)
            {
                try
                {
                    value = _encryptor.Decrypt(value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt value for key: {Key}", entry.Key);
                    value = null;
                }
            }

            var configKey = KeyNormalizer.ToConfigurationKey(entry.Key);
            data[configKey] = value;
        }

        Data = data;
        _logger.LogInformation("Loaded {Count} configuration keys from database (environment: {Environment})",
            data.Count, _options.Environment);
    }

    private IDbAppSettingsStorageEngine CreateDefaultEfEngine()
    {
        var dbContextOptions = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite(_options.ConnectionString)
            .Options;

        return new EfCoreStorageEngine(() => new AppSettingsDbContext(dbContextOptions));
    }

    public override bool TryGet(string key, out string? value)
    {
        if (base.TryGet(key, out value))
        {
            return true;
        }

        var normalizedKey = KeyNormalizer.ToConfigurationKey(key);
        return base.TryGet(normalizedKey, out value);
    }

    public override void Set(string key, string? value)
    {
        var configKey = KeyNormalizer.ToConfigurationKey(key);
        Data[configKey] = value;
        OnReload();
    }

    public void TriggerReload() => OnReload();
}
