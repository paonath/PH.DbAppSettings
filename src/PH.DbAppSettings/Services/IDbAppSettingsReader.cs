namespace PH.DbAppSettings.Services;

public interface IDbAppSettingsReader
{
    T? Get<T>(string? sectionKey = null);
    T GetValue<T>(string key, T defaultValue = default!);
}
