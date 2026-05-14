using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Encryption;

namespace PH.DbAppSettings.Services;

public sealed class SeedService
{
    private readonly AppSettingsDbContext _dbContext;
    private readonly DbAppSettingsOptions _options;
    private readonly ILogger<SeedService> _logger;
    private readonly IValueEncryptor? _encryptor;

    public SeedService(
        AppSettingsDbContext dbContext,
        DbAppSettingsOptions options,
        ILogger<SeedService> logger,
        IValueEncryptor? encryptor = null)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
        _encryptor = encryptor;
    }

    public async Task SeedAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        var allEntries = configuration.AsEnumerable(makePathsRelative: false)
            .Where(kvp => kvp.Value is not null)
            .ToList();

        var excludeSet = new HashSet<string>(_options.ExcludeKeysFromSeed, StringComparer.OrdinalIgnoreCase);

        int seeded = 0;
        int skipped = 0;

        foreach (var (rawKey, rawValue) in allEntries)
        {
            if (excludeSet.Contains(rawKey))
            {
                _logger.LogDebug("Skipping excluded key: {Key}", rawKey);
                skipped++;
                continue;
            }

            var dbKey = rawKey.Replace(":", "__");
            var value = rawValue;
            var isEncrypted = false;

            if (_options.EncryptValues && _encryptor is not null && value is not null)
            {
                value = _encryptor.Encrypt(value);
                isEncrypted = true;
            }

            if (_options.ForceReseed)
            {
                var existing = await _dbContext.AppSettings
                    .FirstOrDefaultAsync(e => e.Key == dbKey && e.Environment == _options.Environment, ct);

                if (existing is not null)
                {
                    existing.Value = value;
                    existing.IsEncrypted = isEncrypted;
                    _logger.LogDebug("Force-reseeding key: {Key}", dbKey);
                }
                else
                {
                    _dbContext.AppSettings.Add(new AppSettingEntry
                    {
                        Key = dbKey,
                        Environment = _options.Environment,
                        Value = value,
                        IsEncrypted = isEncrypted
                    });
                    _logger.LogDebug("Seeding new key: {Key}", dbKey);
                }
            }
            else
            {
                var exists = await _dbContext.AppSettings
                    .AnyAsync(e => e.Key == dbKey && e.Environment == _options.Environment, ct);

                if (!exists)
                {
                    _dbContext.AppSettings.Add(new AppSettingEntry
                    {
                        Key = dbKey,
                        Environment = _options.Environment,
                        Value = value,
                        IsEncrypted = isEncrypted
                    });
                    _logger.LogDebug("Seeding new key: {Key}", dbKey);
                }
                else
                {
                    _logger.LogDebug("Key already exists, skipping: {Key}", dbKey);
                    skipped++;
                    continue;
                }
            }

            seeded++;
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Seeding complete. Seeded: {Seeded}, Skipped: {Skipped}", seeded, skipped);
    }
}
