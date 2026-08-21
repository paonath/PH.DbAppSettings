using System.Text.Json;
using PH.DbAppSettings.Cli.Models;
using PH.DbAppSettings.Configuration;

namespace PH.DbAppSettings.Cli.Services;

public sealed class AppSettingsJsonAnalyzer
{
    private static readonly string[] SensitiveKeywords =
    [
        "Password", "Secret", "Key", "Token", "ConnectionString", "Cert", "Credential", "ApiKey"
    ];

    public AppSettingsAnalysisResult Analyze(string jsonContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);

        using var doc = JsonDocument.Parse(jsonContent);
        var items = new List<FlattenedSettingItem>();

        TraverseElement(doc.RootElement, "", items);

        return new AppSettingsAnalysisResult
        {
            Items = items
        };
    }

    private void TraverseElement(JsonElement element, string currentPath, List<FlattenedSettingItem> items)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = string.IsNullOrEmpty(currentPath)
                        ? property.Name
                        : $"{currentPath}:{property.Name}";
                    TraverseElement(property.Value, childPath, items);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var childPath = $"{currentPath}:{index}";
                    TraverseElement(item, childPath, items);
                    index++;
                }
                break;

            case JsonValueKind.String:
                AddLeafItem(currentPath, element.GetString(), "String", items);
                break;

            case JsonValueKind.Number:
                AddLeafItem(currentPath, element.GetRawText(), "Number", items);
                break;

            case JsonValueKind.True:
                AddLeafItem(currentPath, "true", "Boolean", items);
                break;

            case JsonValueKind.False:
                AddLeafItem(currentPath, "false", "Boolean", items);
                break;

            case JsonValueKind.Null:
                AddLeafItem(currentPath, null, "Null", items);
                break;
        }
    }

    private void AddLeafItem(string rawKey, string? value, string valueType, List<FlattenedSettingItem> items)
    {
        if (string.IsNullOrWhiteSpace(rawKey)) return;

        var dbKey = KeyNormalizer.ToDbKey(rawKey);
        var isSensitive = DetectSensitive(rawKey);

        items.Add(new FlattenedSettingItem
        {
            RawKey = rawKey,
            DbKey = dbKey,
            Value = value,
            ValueType = valueType,
            IsSensitive = isSensitive
        });
    }

    private static bool DetectSensitive(string key)
    {
        var segments = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            foreach (var keyword in SensitiveKeywords)
            {
                if (segment.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
