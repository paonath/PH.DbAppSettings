namespace PH.DbAppSettings.Storage;

/// <summary>
/// Contratto per il motore di memorizzazione e recupero dei settaggi di configurazione.
/// </summary>
public interface IDbAppSettingsStorageEngine
{
    Task<IReadOnlyList<AppSettingRecord>> GetAllAsync(string environment, CancellationToken ct = default);
    Task<AppSettingRecord?> GetByKeyAsync(string key, string environment, CancellationToken ct = default);
    Task UpsertAsync(AppSettingRecord entry, CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<AppSettingRecord> entries, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, string environment, CancellationToken ct = default);
    Task<bool> IsEmptyAsync(string environment, CancellationToken ct = default);
    Task EnsureSchemaCreatedAsync(CancellationToken ct = default);
    Task<DateTimeOffset?> GetLastModifiedTimestampAsync(string environment, CancellationToken ct = default);
}
