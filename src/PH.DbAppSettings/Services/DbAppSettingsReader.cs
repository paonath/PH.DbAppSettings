using Microsoft.Extensions.Configuration;

namespace PH.DbAppSettings.Services;

internal sealed class DbAppSettingsReader(IConfiguration configuration) : IDbAppSettingsReader
{
    public T? Get<T>(string? sectionKey = null)
        => sectionKey is null
            ? configuration.Get<T>()
            : configuration.GetSection(sectionKey.Replace("__", ":")).Get<T>();

    public T GetValue<T>(string key, T defaultValue = default!)
    {
        var section = configuration.GetSection(key.Replace("__", ":"));
        if (section.Exists() && section.Value is not null)
        {
            return configuration.GetValue<T>(key.Replace("__", ":"), defaultValue)!;
        }

        return configuration.GetValue<T>(key, defaultValue)!;
    }
}
