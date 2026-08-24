namespace PH.DbAppSettings.Cli.Models;

public sealed record AppSettingsAnalysisResult
{
    public required IReadOnlyList<FlattenedSettingItem> Items { get; init; }
    public int TotalCount => Items.Count;
    public int TotalKeys => Items.Count;
    public int SensitiveCount => Items.Count(i => i.IsSensitive);
    public int SensitiveKeysCount => Items.Count(i => i.IsSensitive);
}
