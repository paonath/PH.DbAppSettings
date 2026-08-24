namespace PH.DbAppSettings.Storage;

/// <summary>
/// Record immutabile rappresentante una singola voce di configurazione nel database.
/// </summary>
public sealed record AppSettingRecord
{
    public required string Key { get; init; }
    public string Environment { get; init; } = "Production";
    public string? Value { get; init; }
    public bool IsEncrypted { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
