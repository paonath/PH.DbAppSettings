using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Encryption;
using PH.DbAppSettings.Storage;

namespace PH.DbAppSettings.Services;

public sealed class SeedService
{
    private readonly IDbAppSettingsStorageEngine _storageEngine;
    private readonly DbAppSettingsOptions _options;
    private readonly ILogger<SeedService> _logger;
    private readonly IValueEncryptor? _encryptor;

    public SeedService(
        IDbAppSettingsStorageEngine storageEngine,
        DbAppSettingsOptions options,
        ILogger<SeedService> logger,
        IValueEncryptor? encryptor = null)
    {
        _storageEngine = storageEngine ?? throw new ArgumentNullException(nameof(storageEngine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _encryptor = encryptor;
    }

    public SeedService(
        AppSettingsDbContext dbContext,
        DbAppSettingsOptions options,
        ILogger<SeedService> logger,
        IValueEncryptor? encryptor = null)
        : this(new EfCoreStorageEngine(dbContext), options, logger, encryptor)
    {
    }

    public async Task SeedAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        var allEntries = configuration.AsEnumerable(makePathsRelative: false)
            .Where(kvp => kvp.Value is not null)
            .ToList();

        var excludeSet = new HashSet<string>(_options.ExcludeKeysFromSeed, StringComparer.OrdinalIgnoreCase);

        int seeded = 0;
        int skipped = 0;
        var recordsToUpsert = new List<AppSettingRecord>();

        foreach (var (rawKey, rawValue) in allEntries)
        {
            if (excludeSet.Contains(rawKey))
            {
                _logger.LogDebug("Skipping excluded key: {Key}", rawKey);
                skipped++;
                continue;
            }

            var dbKey = KeyNormalizer.ToDbKey(rawKey);
            var value = rawValue;
            var isEncrypted = false;

            if (_options.EncryptValues && _encryptor is not null && value is not null)
            {
                value = _encryptor.Encrypt(value);
                isEncrypted = true;
            }

            if (!_options.ForceReseed)
            {
                var existing = await _storageEngine.GetByKeyAsync(dbKey, _options.Environment, ct);
                if (existing is not null)
                {
                    _logger.LogDebug("Key already exists, skipping: {Key}", dbKey);
                    skipped++;
                    continue;
                }
            }

            recordsToUpsert.Add(new AppSettingRecord
            {
                Key = dbKey,
                Environment = _options.Environment,
                Value = value,
                IsEncrypted = isEncrypted,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            seeded++;
        }

        if (recordsToUpsert.Count > 0)
        {
            await _storageEngine.UpsertBatchAsync(recordsToUpsert, ct);
        }

        _logger.LogInformation("Seeding complete. Seeded: {Seeded}, Skipped: {Skipped}", seeded, skipped);
    }
}
