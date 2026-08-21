using Microsoft.Extensions.Logging;
using PH.DbAppSettings.Configuration;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Encryption;
using PH.DbAppSettings.Storage;

namespace PH.DbAppSettings.Services;

public sealed class DbAppSettingsWriter : IDbAppSettingsWriter
{
    private readonly IDbAppSettingsStorageEngine _storageEngine;
    private readonly DbAppSettingsOptions _options;
    private readonly ILogger<DbAppSettingsWriter> _logger;
    private readonly IValueEncryptor? _encryptor;

    public DbAppSettingsWriter(
        IDbAppSettingsStorageEngine storageEngine,
        DbAppSettingsOptions options,
        ILogger<DbAppSettingsWriter> logger,
        IValueEncryptor? encryptor = null)
    {
        _storageEngine = storageEngine ?? throw new ArgumentNullException(nameof(storageEngine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _encryptor = encryptor;
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var dbKey = KeyNormalizer.ToDbKey(key);
        var isEncrypted = false;
        var storedValue = value;

        if (_options.EncryptValues && _encryptor is not null && value is not null)
        {
            storedValue = _encryptor.Encrypt(value);
            isEncrypted = true;
        }

        var entry = new AppSettingRecord
        {
            Key = dbKey,
            Environment = _options.Environment,
            Value = storedValue,
            IsEncrypted = isEncrypted,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _storageEngine.UpsertAsync(entry, ct);
        _logger.LogDebug("Saved key: {Key} in environment: {Environment}", dbKey, _options.Environment);
    }

    public Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        var stringValue = value is null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        return SetAsync(key, stringValue, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var dbKey = KeyNormalizer.ToDbKey(key);
        var deleted = await _storageEngine.DeleteAsync(dbKey, _options.Environment, ct);

        if (deleted)
        {
            _logger.LogDebug("Deleted key: {Key} in environment: {Environment}", dbKey, _options.Environment);
        }
        else
        {
            _logger.LogWarning("Key not found for deletion: {Key} in environment: {Environment}", dbKey, _options.Environment);
        }
    }
}
