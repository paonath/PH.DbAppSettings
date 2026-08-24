namespace PH.DbAppSettings.Cli.Models;

public sealed record FlattenedSettingItem
{
    public required string RawKey { get; init; }
    public required string DbKey { get; init; }
    public string Key => DbKey;
    public string? Value { get; init; }
    public required string ValueType { get; init; }
    public bool IsSensitive { get; init; }
}
