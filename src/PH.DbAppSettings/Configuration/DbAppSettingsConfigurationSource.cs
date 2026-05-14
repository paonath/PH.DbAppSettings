using Microsoft.Extensions.Configuration;
using PH.DbAppSettings.Encryption;

namespace PH.DbAppSettings.Configuration;

public sealed class DbAppSettingsConfigurationSource : IConfigurationSource
{
    private readonly DbAppSettingsOptions _options;
    private readonly IConfiguration? _bootstrapConfig;
    private readonly IValueEncryptor? _encryptor;

    public DbAppSettingsConfigurationSource(
        DbAppSettingsOptions options,
        IConfiguration? bootstrapConfig = null,
        IValueEncryptor? encryptor = null)
    {
        _options = options;
        _bootstrapConfig = bootstrapConfig;
        _encryptor = encryptor;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new DbAppSettingsProvider(_options, _bootstrapConfig, encryptor: _encryptor);
}
