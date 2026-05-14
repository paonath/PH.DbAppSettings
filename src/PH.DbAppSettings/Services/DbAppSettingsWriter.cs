using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PH.DbAppSettings.Data;
using PH.DbAppSettings.Encryption;

namespace PH.DbAppSettings.Services;

public sealed class DbAppSettingsWriter : IDbAppSettingsWriter
{
    private readonly AppSettingsDbContext _dbContext;
    private readonly DbAppSettingsOptions _options;
    private readonly ILogger<DbAppSettingsWriter> _logger;
    private readonly IValueEncryptor? _encryptor;

    public DbAppSettingsWriter(
        AppSettingsDbContext dbContext,
        DbAppSettingsOptions options,
        ILogger<DbAppSettingsWriter> logger,
        IValueEncryptor? encryptor = null)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
        _encryptor = encryptor;
    }

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var isEncrypted = false;
        var storedValue = value;

        if (_options.EncryptValues && _encryptor is not null && value is not null)
        {
            storedValue = _encryptor.Encrypt(value);
            isEncrypted = true;
        }

        var existing = await _dbContext.AppSettings
            .FirstOrDefaultAsync(e => e.Key == key && e.Environment == _options.Environment, ct);

        if (existing is not null)
        {
            existing.Value = storedValue;
            existing.IsEncrypted = isEncrypted;
            _logger.LogDebug("Updated key: {Key} in environment: {Environment}", key, _options.Environment);
        }
        else
        {
            _dbContext.AppSettings.Add(new AppSettingEntry
            {
                Key = key,
                Environment = _options.Environment,
                Value = storedValue,
                IsEncrypted = isEncrypted
            });
            _logger.LogDebug("Inserted key: {Key} in environment: {Environment}", key, _options.Environment);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        var stringValue = value is null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        return SetAsync(key, stringValue, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var existing = await _dbContext.AppSettings
            .FirstOrDefaultAsync(e => e.Key == key && e.Environment == _options.Environment, ct);

        if (existing is not null)
        {
            _dbContext.AppSettings.Remove(existing);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogDebug("Deleted key: {Key} in environment: {Environment}", key, _options.Environment);
        }
        else
        {
            _logger.LogWarning("Key not found for deletion: {Key} in environment: {Environment}", key, _options.Environment);
        }
    }
}
