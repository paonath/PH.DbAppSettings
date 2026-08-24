using System.Text.Json;
using PH.DbAppSettings.Cli;
using PH.DbAppSettings.Cli.Models;

namespace PH.DbAppSettings.Tests;

public class UnifiedJsonAnalyzerTests
{
    [Fact]
    public void Analyze_FlattensNestedJson_AndIdentifiesSensitiveKeys()
    {
        // Arrange
        var json = """
        {
            "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Database=mydb;User Id=sa;Password=secret;"
            },
            "Application": {
                "Title": "My Test App",
                "Port": 5000,
                "ApiKey": "secret-api-key-12345",
                "Features": {
                    "EnableCache": true
                }
            }
        }
        """;

        // Act
        var result = AppSettingsJsonAnalyzer.Analyze(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.SensitiveCount);

        var connStr = result.Items.FirstOrDefault(i => i.Key == "ConnectionStrings__DefaultConnection");
        Assert.NotNull(connStr);
        Assert.True(connStr.IsSensitive);

        var apiKey = result.Items.FirstOrDefault(i => i.Key == "Application__ApiKey");
        Assert.NotNull(apiKey);
        Assert.True(apiKey.IsSensitive);

        var title = result.Items.FirstOrDefault(i => i.Key == "Application__Title");
        Assert.NotNull(title);
        Assert.False(title.IsSensitive);
        Assert.Equal("My Test App", title.Value);
    }

    [Fact]
    public void JsonTreeReconstructor_ReconstructsTypedJsonHierarchy()
    {
        // Arrange
        var records = new List<Storage.AppSettingRecord>
        {
            new() { Key = "Application__Title", Environment = "Test", Value = "My Test App" },
            new() { Key = "Application__Port", Environment = "Test", Value = "5000" },
            new() { Key = "Application__Features__EnableCache", Environment = "Test", Value = "true" }
        };

        // Act
        var reconstructedJson = JsonTreeReconstructor.Reconstruct(records);
        using var doc = JsonDocument.Parse(reconstructedJson);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("My Test App", root.GetProperty("Application").GetProperty("Title").GetString());
        Assert.Equal(5000, root.GetProperty("Application").GetProperty("Port").GetInt32());
        Assert.True(root.GetProperty("Application").GetProperty("Features").GetProperty("EnableCache").GetBoolean());
    }
}
