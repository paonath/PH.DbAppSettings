namespace PH.DbAppSettings.Services;

public interface IDbAppSettingsWriter
{
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
