using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PH.DbAppSettings.Configuration;

namespace PH.DbAppSettings.Cli.Services;

public static class JsonTreeReconstructor
{
    public static string ReconstructJson(IEnumerable<KeyValuePair<string, string?>> entries)
    {
        var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rawKey, value) in entries)
        {
            var configKey = KeyNormalizer.ToConfigurationKey(rawKey);
            var segments = configKey.Split(':');
            InsertIntoDictionary(root, segments, 0, value);
        }

        var jsonNode = ConvertToJsonNode(root);
        var options = new JsonSerializerOptions { WriteIndented = true };
        return jsonNode?.ToJsonString(options) ?? "{}";
    }

    private static void InsertIntoDictionary(
        Dictionary<string, object?> currentDict,
        string[] segments,
        int index,
        string? value)
    {
        var segment = segments[index];

        if (index == segments.Length - 1)
        {
            currentDict[segment] = ParseTypedValue(value);
            return;
        }

        if (!currentDict.TryGetValue(segment, out var child) || child is not Dictionary<string, object?> childDict)
        {
            childDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            currentDict[segment] = childDict;
        }

        InsertIntoDictionary(childDict, segments, index + 1, value);
    }

    private static object? ParseTypedValue(string? value)
    {
        if (value is null) return null;

        if (bool.TryParse(value, out var boolVal))
        {
            return boolVal;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal))
        {
            return longVal;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal) && value.Contains('.'))
        {
            return doubleVal;
        }

        return value;
    }

    private static JsonNode? ConvertToJsonNode(object? obj)
    {
        if (obj is null) return null;

        if (obj is bool b) return JsonValue.Create(b);
        if (obj is long l) return JsonValue.Create(l);
        if (obj is int i) return JsonValue.Create(i);
        if (obj is double d) return JsonValue.Create(d);
        if (obj is string s) return JsonValue.Create(s);

        if (obj is Dictionary<string, object?> dict)
        {
            // Check if all keys are numeric array indexes: 0, 1, 2, ...
            var keys = dict.Keys.ToList();
            if (keys.Count > 0 && keys.All(k => int.TryParse(k, out _)))
            {
                var sorted = keys.Select(k => (Index: int.Parse(k), Key: k))
                    .OrderBy(x => x.Index)
                    .ToList();

                var isSequential = true;
                for (var idx = 0; idx < sorted.Count; idx++)
                {
                    if (sorted[idx].Index != idx)
                    {
                        isSequential = false;
                        break;
                    }
                }

                if (isSequential)
                {
                    var jsonArray = new JsonArray();
                    foreach (var item in sorted)
                    {
                        jsonArray.Add(ConvertToJsonNode(dict[item.Key]));
                    }
                    return jsonArray;
                }
            }

            var jsonObj = new JsonObject();
            foreach (var kvp in dict)
            {
                jsonObj[kvp.Key] = ConvertToJsonNode(kvp.Value);
            }
            return jsonObj;
        }

        return JsonValue.Create(obj.ToString());
    }
}
