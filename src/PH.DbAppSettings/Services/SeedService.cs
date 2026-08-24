using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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
        var entriesToSeed = ExtractSeedableEntries(configuration).ToList();

        int seeded = 0;
        int skipped = 0;
        var recordsToUpsert = new List<AppSettingRecord>();

        foreach (var (rawKey, rawValue) in entriesToSeed)
        {
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

    private IEnumerable<KeyValuePair<string, string?>> ExtractSeedableEntries(IConfiguration configuration)
    {
        var excludeSet = new HashSet<string>(_options.ExcludeKeysFromSeed, StringComparer.OrdinalIgnoreCase);

        // Strategy 1: If IConfigurationRoot, extract exclusively from FileConfigurationProvider / JsonConfigurationProvider
        if (configuration is IConfigurationRoot root)
        {
            var fileProviders = root.Providers
                .Where(p => p is FileConfigurationProvider || p.GetType().Name.Contains("JsonConfigurationProvider", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (fileProviders.Count > 0)
            {
                var fileData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var provider in fileProviders)
                {
                    ExtractKeysRecursively(provider, null, fileData);
                }

                return fileData
                    .Where(kvp => kvp.Value is not null)
                    .Where(kvp => !excludeSet.Contains(kvp.Key))
                    .Where(kvp => !IsKnownBootstrapKey(kvp.Key));
            }
        }

        // Strategy 2: Fallback when not IConfigurationRoot or no File providers:
        // Filter out process and OS environment variables
        var envVars = System.Environment.GetEnvironmentVariables()
            .Keys
            .Cast<object>()
            .Select(k => k.ToString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return configuration.AsEnumerable(makePathsRelative: false)
            .Where(kvp => kvp.Value is not null)
            .Where(kvp => !excludeSet.Contains(kvp.Key))
            .Where(kvp => !IsKnownBootstrapKey(kvp.Key))
            .Where(kvp => !IsEnvironmentVariable(kvp.Key, envVars));
    }

    private static void ExtractKeysRecursively(
        IConfigurationProvider provider,
        string? parentPath,
        IDictionary<string, string?> result)
    {
        var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), parentPath);
        foreach (var key in childKeys)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}:{key}";
            if (provider.TryGet(currentPath, out var value) && value is not null)
            {
                result[currentPath] = value;
            }

            ExtractKeysRecursively(provider, currentPath, result);
        }
    }

    private static bool IsKnownBootstrapKey(string key)
    {
        var normalized = key.Replace(":", "__");
        return normalized.StartsWith("DbAppSettings__", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("ASPNETCORE_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("DOTNET_ENVIRONMENT", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnvironmentVariable(string key, HashSet<string> envVars)
    {
        if (envVars.Contains(key) || envVars.Contains(key.Replace(":", "__")))
        {
            return true;
        }

        var rootSegment = key.Split(':')[0];
        if (!key.Contains(':') && envVars.Contains(rootSegment))
        {
            return true;
        }

        return false;
    }
}
