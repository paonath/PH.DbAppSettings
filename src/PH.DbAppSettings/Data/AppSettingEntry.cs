namespace PH.DbAppSettings.Data;

public sealed class AppSettingEntry
{
    public required string Key { get; set; }
    public string Environment { get; set; } = "Production";
    public string? Value { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
