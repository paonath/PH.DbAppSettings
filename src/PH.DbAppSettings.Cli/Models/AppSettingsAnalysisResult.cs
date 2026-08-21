namespace PH.DbAppSettings.Cli.Models;

public sealed record AppSettingsAnalysisResult
{
    public required IReadOnlyList<FlattenedSettingItem> Items { get; init; }
    public int TotalKeys => Items.Count;
    public int SensitiveKeysCount => Items.Count(i => i.IsSensitive);
}
